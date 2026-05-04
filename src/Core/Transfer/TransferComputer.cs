using KspNavComputer.Core.Bodies;
using KspNavComputer.Core.Mechanics;
using System.Collections.Generic;

namespace KspNavComputer.Core.Transfer;

/// <summary>
/// Computes one-way interplanetary transfer Δv for circular, equatorial
/// parking orbits (Increment 1a).
///
/// Algorithm:
///   1. Propagate origin and destination bodies to their positions at
///      departure and arrival UT using Kepler's equations.
///   2. Solve Lambert's problem for the heliocentric transfer arc.
///   3. Convert heliocentric hyperbolic-excess velocity to a parking-orbit
///      ejection/insertion Δv using the vis-viva + hyperbolic excess method.
/// </summary>
public static class TransferComputer
{
    /// <summary>
    /// Computes the transfer Δv budget for the supplied parameters.
    /// </summary>
    /// <exception cref="ArgumentException">
    ///   Origin and destination must share the same parent body (same SOI parent).
    /// </exception>
    public static TransferResult Compute(TransferParameters p)
    {
        // Both bodies must orbit the same central body (e.g. both orbit Kerbol)
        if (p.Origin.Parent is null || p.Destination.Parent is null
            || !ReferenceEquals(p.Origin.Parent, p.Destination.Parent))
        {
            throw new ArgumentException(
                "Origin and Destination must both orbit the same parent body.");
        }

        if (ReferenceEquals(p.Origin, p.Destination))
            throw new ArgumentException("Origin and Destination must be different bodies.");

        CelestialBody centralBody = p.Origin.Parent;
        double mu = centralBody.GravParam;

        double departureUT = p.DepartureUT;
        double arrivalUT   = p.DepartureUT + p.TimeOfFlight;

        // ---- 1. Planetary state vectors at departure and arrival ----
        var (r1, v1Body) = KeplerSolver.StateAt(p.Origin.Orbit!,      mu, departureUT);
        var (r2, v2Body) = KeplerSolver.StateAt(p.Destination.Orbit!, mu, arrivalUT);

        // ---- 2. Lambert solver → heliocentric transfer velocities ----
        // Enumerate all solutions (0-rev and multi-rev, both prograde and retrograde)
        // and keep the minimum total Δv, matching LWP's approach.
        var allSolutions = new List<(Vector3d V1, Vector3d V2)>();
        allSolutions.AddRange(LambertSolver.SolveAllRevolutions(r1, r2, p.TimeOfFlight, mu, maxRevs: 10, prograde: true));
        allSolutions.AddRange(LambertSolver.SolveAllRevolutions(r1, r2, p.TimeOfFlight, mu, maxRevs: 10, prograde: false));

        double bestDvEject = double.MaxValue, bestDvInsert = double.MaxValue;
        foreach (var (vT1, vT2) in allSolutions)
        {
            double dve = ComputeManeuverDv(p.OriginOrbit,      p.Origin,      vT1, v1Body, isEjection: true);
            double dvi = ComputeManeuverDv(p.DestinationOrbit, p.Destination, vT2, v2Body, isEjection: false);
            if (dve + dvi < bestDvEject + bestDvInsert)
            {
                bestDvEject  = dve;
                bestDvInsert = dvi;
            }
        }

        double dvEject  = bestDvEject;
        double dvInsert = bestDvInsert;

        return new TransferResult(
            DepartureUT:      departureUT,
            ArrivalUT:        arrivalUT,
            EjectionDeltaV:   dvEject,
            InsertionDeltaV:  dvInsert,
            TotalDeltaV:      dvEject + dvInsert
        );
    }

    /// <summary>
    /// Computes the round-trip Δv budget: outbound leg then return leg.
    ///
    /// The return leg departs the destination at <c>arrivalUT + stayDuration</c>
    /// and travels back to the origin.  Origin and destination parking orbits
    /// are reused for both legs (the same orbit used for capture on arrival is
    /// used as the departure orbit on the way home, and vice-versa).
    /// </summary>
    public static RoundTripResult ComputeRoundTrip(RoundTripParameters p)
    {
        var outboundParams = new TransferParameters(
            Origin:           p.Origin,
            Destination:      p.Destination,
            DepartureUT:      p.DepartureUT,
            TimeOfFlight:     p.OutboundTimeOfFlight,
            OriginOrbit:      p.OriginOrbit,
            DestinationOrbit: p.DestinationOrbit
        );
        var outbound = Compute(outboundParams);

        double returnDeparture = outbound.ArrivalUT + p.StayDuration;
        var returnParams = new TransferParameters(
            Origin:           p.Destination,
            Destination:      p.Origin,
            DepartureUT:      returnDeparture,
            TimeOfFlight:     p.ReturnTimeOfFlight,
            OriginOrbit:      p.DestinationOrbit,
            DestinationOrbit: p.OriginOrbit
        );
        var ret = Compute(returnParams);

        return new RoundTripResult(
            Outbound:   outbound,
            Return:     ret,
            TotalDeltaV: outbound.TotalDeltaV + ret.TotalDeltaV
        );
    }

    // -------------------------------------------------------------------------
    // Helpers
    // -------------------------------------------------------------------------

    /// <summary>
    /// Converts a heliocentric transfer velocity vector into a parking-orbit
    /// ejection (or capture) Δv, including the plane-change cost when the
    /// departure/arrival hyperbola is inclined relative to the parking orbit.
    ///
    /// Matches alexmoon's LWP <c>circularToEscapeDeltaV</c> /
    /// <c>insertionToCircularDeltaV</c> with inclination term.
    ///
    /// Steps:
    ///   1. v∞ = |v_transfer − v_body|
    ///   2. v_p = √(v∞² + 2·v_c² − 2·μ/r_SOI)  (periapsis speed of hyperbola)
    ///   3. Without inclination: Δv = v_p − v_c
    ///   4. With inclination α:  Δv = √(v_c² + v_p² − 2·v_c·v_p·cos α)
    ///      where α is the inclination of the departure/arrival hyperbola
    ///      relative to the parking orbit plane.
    /// </summary>
    private static double ComputeManeuverDv(
        ParkingOrbit parkingOrbit, CelestialBody body,
        Vector3d vTransfer, Vector3d vBody, bool isEjection)
    {
        double muBody = body.GravParam;
        double r      = body.Radius + parkingOrbit.Altitude;
        double rSoi   = body.SphereOfInfluence;
        double vCirc  = Math.Sqrt(muBody / r);

        // v_inf and periapsis speed
        Vector3d vInfVec = vTransfer - vBody;
        double   vInf    = vInfVec.Magnitude;
        double   vPeri   = Math.Sqrt(vInf * vInf + 2.0 * vCirc * vCirc - 2.0 * muBody / rSoi);

        // Ejection inclination: angle of the departure hyperbola's plane
        // relative to the ecliptic (equatorial parking orbit plane).
        // LWP applies this only for ejection (circularToEscapeDeltaV), not
        // insertion (insertionToCircularDeltaV has no inclination term).
        if (!isEjection || Math.Abs(vInfVec.Z) < 1e-9)
        {
            return Math.Abs(vPeri - vCirc);
        }
        double inclination = Math.Asin(Math.Clamp(vInfVec.Z / vInf, -1.0, 1.0));
        return Math.Sqrt(vCirc * vCirc + vPeri * vPeri
                         - 2.0 * vCirc * vPeri * Math.Cos(inclination));
    }
}
