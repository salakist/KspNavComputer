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

        Burn bestEject  = new(double.MaxValue, 0.0, BurnVector.Zero);
        Burn bestInsert = new(double.MaxValue, 0.0, BurnVector.Zero);

        foreach (var (vT1, vT2) in allSolutions)
        {
            var eject  = ManeuverCalculator.Compute(p.OriginOrbit,      p.Origin,      vT1, v1Body, isEjection: true,  refUT: departureUT);
            var insert = ManeuverCalculator.Compute(p.DestinationOrbit, p.Destination, vT2, v2Body, isEjection: false, refUT: arrivalUT);
            if (eject.DeltaV + insert.DeltaV < bestEject.DeltaV + bestInsert.DeltaV)
            {
                bestEject  = eject;
                bestInsert = insert;
            }
        }

        return new TransferResult(
            DepartureUT:  departureUT,
            ArrivalUT:    arrivalUT,
            Ejection:     bestEject,
            Insertion:    bestInsert,
            TotalDeltaV:  bestEject.DeltaV + bestInsert.DeltaV
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

}
