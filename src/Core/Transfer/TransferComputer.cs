using KspNavComputer.Core.Bodies;
using KspNavComputer.Core.Mechanics;

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

        CelestialBody centralBody = p.Origin.Parent;
        double mu = centralBody.GravParam;

        double departureUT = p.DepartureUT;
        double arrivalUT   = p.DepartureUT + p.TimeOfFlight;

        // ---- 1. Planetary state vectors at departure and arrival ----
        var (r1, v1Body) = KeplerSolver.StateAt(p.Origin.Orbit!,      mu, departureUT);
        var (r2, v2Body) = KeplerSolver.StateAt(p.Destination.Orbit!, mu, arrivalUT);

        // ---- 2. Lambert solver → heliocentric transfer velocities ----
        var (vTransfer1, vTransfer2) = LambertSolver.Solve(r1, r2, p.TimeOfFlight, mu);

        // ---- 3a. Ejection Δv (departure body) ----
        double dvEject = ComputeManeuverDv(
            parkingOrbit:  p.OriginOrbit,
            body:          p.Origin,
            vTransfer:     vTransfer1,
            vBody:         v1Body
        );

        // ---- 3b. Insertion Δv (destination body) ----
        double dvInsert = ComputeManeuverDv(
            parkingOrbit:  p.DestinationOrbit,
            body:          p.Destination,
            vTransfer:     vTransfer2,
            vBody:         v2Body
        );

        return new TransferResult(
            DepartureUT:      departureUT,
            ArrivalUT:        arrivalUT,
            EjectionDeltaV:   dvEject,
            InsertionDeltaV:  dvInsert,
            TotalDeltaV:      dvEject + dvInsert
        );
    }

    // -------------------------------------------------------------------------
    // Helpers
    // -------------------------------------------------------------------------

    /// <summary>
    /// Converts a heliocentric transfer velocity vector into a parking-orbit
    /// ejection (or capture) Δv.
    ///
    /// The v-infinity (hyperbolic excess) at the body is:
    ///   v∞ = |v_transfer − v_body|
    ///
    /// For a circular parking orbit at radius r = R_body + altitude, the
    /// circular speed is v_c = √(μ_body / r) and the periapsis speed of the
    /// hyperbolic escape trajectory is v_p = √(v∞² + 2·μ_body/r).
    /// The required Δv = v_p − v_c.
    /// </summary>
    private static double ComputeManeuverDv(
        ParkingOrbit parkingOrbit, CelestialBody body,
        Vector3d vTransfer, Vector3d vBody)
    {
        double vInf   = (vTransfer - vBody).Magnitude;
        double muBody = body.GravParam;
        double r      = body.Radius + parkingOrbit.Altitude;
        double vCirc  = Math.Sqrt(muBody / r);
        double vPeri  = Math.Sqrt(vInf * vInf + 2.0 * muBody / r);
        return Math.Abs(vPeri - vCirc);
    }
}
