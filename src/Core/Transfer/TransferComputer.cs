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
        // Try both prograde (counter-clockwise) and retrograde (clockwise) arcs
        // and keep the solution with the lower total Δv.  This mirrors what
        // alexmoon's LWP and TriggerAu's TWP do: both evaluate all solutions
        // and return the minimum-Δv option.
        var (vT1p, vT2p) = LambertSolver.Solve(r1, r2, p.TimeOfFlight, mu, prograde: true);
        var (vT1r, vT2r) = LambertSolver.Solve(r1, r2, p.TimeOfFlight, mu, prograde: false);

        double dvEjectP  = ComputeManeuverDv(p.OriginOrbit,      p.Origin,      vT1p, v1Body);
        double dvInsertP = ComputeManeuverDv(p.DestinationOrbit, p.Destination, vT2p, v2Body);

        double dvEjectR  = ComputeManeuverDv(p.OriginOrbit,      p.Origin,      vT1r, v1Body);
        double dvInsertR = ComputeManeuverDv(p.DestinationOrbit, p.Destination, vT2r, v2Body);

        // ---- 3. Pick cheaper arc ----
        double dvEject, dvInsert;
        if (dvEjectP + dvInsertP <= dvEjectR + dvInsertR)
        {
            dvEject  = dvEjectP;
            dvInsert = dvInsertP;
        }
        else
        {
            dvEject  = dvEjectR;
            dvInsert = dvInsertR;
        }

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
    /// The v-infinity (hyperbolic excess) at the SOI boundary is:
    ///   v∞ = |v_transfer − v_body|
    ///
    /// For a circular parking orbit at radius r = R_body + altitude the
    /// circular speed is v_c = √(μ_body / r).  The periapsis speed of the
    /// hyperbolic escape trajectory, corrected for the finite SOI radius, is:
    ///   v_p = √(v∞² + 2·μ_body/r − 2·μ_body/r_SOI)
    /// The required Δv = v_p − v_c.
    ///
    /// The SOI correction (−2μ/r_SOI) matches the formulas used by both
    /// alexmoon's Launch Window Planner and TriggerAu's Transfer Window Planner.
    /// Omitting it overestimates Δv by ~10–15 m/s for Kerbin departures.
    /// </summary>
    private static double ComputeManeuverDv(
        ParkingOrbit parkingOrbit, CelestialBody body,
        Vector3d vTransfer, Vector3d vBody)
    {
        double vInf   = (vTransfer - vBody).Magnitude;
        double muBody = body.GravParam;
        double r      = body.Radius + parkingOrbit.Altitude;
        double rSoi   = body.SphereOfInfluence;
        double vCirc  = Math.Sqrt(muBody / r);
        double vPeri  = Math.Sqrt(vInf * vInf + 2.0 * muBody / r - 2.0 * muBody / rSoi);
        return Math.Abs(vPeri - vCirc);
    }
}
