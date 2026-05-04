using KspNavComputer.Core.Bodies;
using KspNavComputer.Core.Mechanics;
using System.Collections.Generic;

namespace KspNavComputer.Core.Transfer;

/// <summary>
/// Computes one-way and round-trip interplanetary transfer Δv, supporting
/// inclined and elliptical parking orbits (Increments 1a–1c).
///
/// Algorithm:
///   1. Propagate origin and destination bodies to their positions at
///      departure and arrival UT using Kepler's equations.
///   2. Solve Lambert's problem for the heliocentric transfer arc.
///   3. Convert heliocentric hyperbolic-excess velocity to a parking-orbit
///      ejection/insertion Δv using the vis-viva + hyperbolic excess method,
///      with eccentricity and inclination corrections.
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
    /// ejection (or capture) Δv.
    ///
    /// Supports:
    /// <list type="bullet">
    ///   <item>Circular and elliptical parking orbits (via eccentricity).</item>
    ///   <item>
    ///     Inclined parking orbits for ejection: the departure hyperbola's
    ///     inclination above the ecliptic is reduced by the parking orbit's
    ///     inclination, because an optimally-timed ejection burn can exploit the
    ///     orbit's tilt.  Effective angle = max(0, |α| − i_park).
    ///   </item>
    /// </list>
    ///
    /// For insertion the inclination term is omitted (matching LWP
    /// <c>insertionToCircularDeltaV</c>); eccentricity is still applied.
    ///
    /// Steps:
    ///   1. v∞ = |v_transfer − v_body|
    ///   2. v_p = √(v∞² + 2μ/r_peri − 2μ/r_SOI)   (hyperbolic periapsis speed)
    ///   3. v_park = √(μ·(1+e)/r_peri)              (parking orbit speed at periapsis)
    ///   4. Δv = |v_p − v_park|  (no inclination)
    ///      or  √(v_park² + v_p² − 2·v_park·v_p·cos Δi)  (with Δi)
    /// </summary>
    private static double ComputeManeuverDv(
        ParkingOrbit parkingOrbit, CelestialBody body,
        Vector3d vTransfer, Vector3d vBody, bool isEjection)
    {
        double muBody = body.GravParam;
        double rPeri  = body.Radius + parkingOrbit.Altitude;
        double rSoi   = body.SphereOfInfluence;
        double e      = parkingOrbit.Eccentricity;

        // Parking orbit speed at periapsis.
        // For circular (e=0): v_park = √(μ/r) = v_circular.
        double vPark = Math.Sqrt(muBody * (1.0 + e) / rPeri);

        // Hyperbolic periapsis speed (vis-viva corrected for SOI boundary).
        Vector3d vInfVec   = vTransfer - vBody;
        double   vInf      = vInfVec.Magnitude;
        double   vHyperPeri = Math.Sqrt(vInf * vInf + 2.0 * muBody / rPeri - 2.0 * muBody / rSoi);

        // Insertion: no inclination term (matches LWP insertionToCircularDeltaV).
        if (!isEjection)
            return Math.Abs(vHyperPeri - vPark);

        // Ejection: no inclination correction when v∞z is negligible.
        if (Math.Abs(vInfVec.Z) < 1e-9 || vInf < 1e-9)
            return Math.Abs(vHyperPeri - vPark);

        // Effective ejection inclination = angle between departure asymptote
        // and ecliptic, reduced by the parking orbit's own inclination.
        // An inclined parking orbit can absorb up to i_park of the plane change
        // for free by timing the ejection burn optimally.
        double alpha  = Math.Asin(Math.Clamp(vInfVec.Z / vInf, -1.0, 1.0));
        double deltaI = Math.Max(0.0, Math.Abs(alpha) - parkingOrbit.Inclination);

        if (deltaI < 1e-9)
            return Math.Abs(vHyperPeri - vPark);

        // Combined ejection + plane-change via law of cosines.
        return Math.Sqrt(vPark * vPark + vHyperPeri * vHyperPeri
                         - 2.0 * vPark * vHyperPeri * Math.Cos(deltaI));
    }
}
