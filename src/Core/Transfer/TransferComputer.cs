using KspNavComputer.Core.Bodies;
using KspNavComputer.Core.Maneuver;
using KspNavComputer.Core.Mechanics;
using System.Collections.Generic;

namespace KspNavComputer.Core.Transfer;

/// <summary>
/// Computes one-way and round-trip interplanetary transfer Δv, supporting
/// Ballistic, MidCoursePlaneChange, and Optimal transfer types, inclined and
/// elliptical parking orbits, and optional fly-by (no insertion burn).
///
/// Algorithm:
///   1. Propagate origin and destination bodies to their positions at
///      departure and arrival UT using Kepler's equations.
///   2. Solve Lambert's problem for the heliocentric transfer arc.
///   3. For Optimal/MidCoursePlaneChange: optionally run the plane-change
///      solver and return the lower total Δv.
///   4. Convert heliocentric hyperbolic-excess velocity to a parking-orbit
///      ejection/insertion Δv using the vis-viva + hyperbolic excess method.
///   5. Populate extra output fields (phase angle, transfer orbit shape, etc.).
/// </summary>
public static class TransferComputer
{
    // -------------------------------------------------------------------------
    // One-way transfer
    // -------------------------------------------------------------------------

    /// <summary>
    /// Computes the transfer Δv budget for the supplied parameters.
    /// </summary>
    /// <exception cref="ArgumentException">
    ///   Origin and destination must share the same parent body (same SOI parent).
    /// </exception>
    public static TransferResult Compute(TransferParameters p)
    {
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

        // ---- 1. Planetary state vectors ----
        var (r1, v1Body) = KeplerSolver.StateAt(p.Origin.Orbit!,      mu, departureUT);
        var (r2, v2Body) = KeplerSolver.StateAt(p.Destination.Orbit!, mu, arrivalUT);

        // ---- 2. Ballistic Lambert solutions ----
        var ballisticSolutions = new List<(Vector3d V1, Vector3d V2)>();
        ballisticSolutions.AddRange(LambertSolver.SolveAllRevolutions(
            r1, r2, p.TimeOfFlight, mu, maxRevs: 10, prograde: true));
        ballisticSolutions.AddRange(LambertSolver.SolveAllRevolutions(
            r1, r2, p.TimeOfFlight, mu, maxRevs: 10, prograde: false));

        // Pick best ballistic solution.
        Burn bestEject  = new(double.MaxValue, 0.0, BurnVector.Zero);
        Burn bestInsert = new(double.MaxValue, 0.0, BurnVector.Zero);
        Vector3d bestVT1 = default, bestVT2 = default;

        foreach (var (vT1, vT2) in ballisticSolutions)
        {
            var ej  = ManeuverComputer.Compute(new ManeuverParameters(
                p.OriginOrbit, p.Origin, vT1, v1Body, true, departureUT));
            var ins = ComputeInsertion(p, vT2, v2Body, arrivalUT);

            if (ej.DeltaV + ins.DeltaV < bestEject.DeltaV + bestInsert.DeltaV)
            {
                bestEject  = ej;
                bestInsert = ins;
                bestVT1    = vT1;
                bestVT2    = vT2;
            }
        }

        double ballisticTotal = bestEject.DeltaV + bestInsert.DeltaV;

        // ---- 3. Optional plane-change ----
        Burn? planeChange = null;

        if (p.TransferType == TransferType.MidCoursePlaneChange
            || p.TransferType == TransferType.Optimal)
        {
            var pcResult = PlaneChangeComputer.Compute(new PlaneChangeParameters(
                r1, v1Body, r2, p.TimeOfFlight, departureUT, mu));

            if (pcResult != null)
            {
                var pcEj  = ManeuverComputer.Compute(new ManeuverParameters(
                    p.OriginOrbit, p.Origin, pcResult.DepartureVelocity, v1Body, true, departureUT));
                var pcIns = ComputeInsertion(p, pcResult.ArrivalVelocity, v2Body, arrivalUT);
                double pcTotal = pcEj.DeltaV + pcResult.PlaneChange.DeltaV + pcIns.DeltaV;

                if (p.TransferType == TransferType.MidCoursePlaneChange
                    || pcTotal < ballisticTotal)
                {
                    bestEject  = pcEj;
                    bestInsert = pcIns;
                    bestVT1    = pcResult.DepartureVelocity;
                    bestVT2    = pcResult.ArrivalVelocity;
                    planeChange = pcResult.PlaneChange;
                }
            }
        }

        double totalDv = bestEject.DeltaV
                       + (planeChange?.DeltaV ?? 0.0)
                       + bestInsert.DeltaV;

        // ---- 4. Extra output fields ----
        var (periapsis, apoapsis) = TransferOrbitShape(r1, bestVT1, mu);
        double centralRadius       = centralBody.Radius;
        double transferAngle       = PlaneChangeComputer.ComputeTransferAngle(r1, r2);
        // Phase angle: both bodies at *departure* time, matching LWP's phaseAngle(orbit, t0)
        var (r2Dep, _)             = KeplerSolver.StateAt(p.Destination.Orbit!, mu, departureUT);
        double phaseAngle          = ComputePhaseAngle(r1, r2Dep, v1Body,
                                                       p.Origin.Orbit!.SemiMajorAxis,
                                                       p.Destination.Orbit!.SemiMajorAxis);
        double insertionInclination = ComputeInsertionInclination(bestVT2, v2Body);

        return new TransferResult(
            DepartureUT:           departureUT,
            ArrivalUT:             arrivalUT,
            Ejection:              bestEject,
            Insertion:             bestInsert,
            TotalDeltaV:           totalDv,
            PlaneChange:           planeChange,
            PhaseAngleDeg:         phaseAngle       * 180.0 / Math.PI,
            TransferAngleDeg:      transferAngle    * 180.0 / Math.PI,
            TransferPeriapsis:     periapsis - centralRadius,
            TransferApoapsis:      apoapsis  - centralRadius,
            InsertionInclinationDeg: insertionInclination * 180.0 / Math.PI
        );
    }

    // -------------------------------------------------------------------------
    // Round-trip transfer
    // -------------------------------------------------------------------------

    /// <summary>
    /// Computes the round-trip Δv budget: outbound leg then return leg.
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
            Outbound:    outbound,
            Return:      ret,
            TotalDeltaV: outbound.TotalDeltaV + ret.TotalDeltaV
        );
    }

    // -------------------------------------------------------------------------
    // Private helpers
    // -------------------------------------------------------------------------

    private static Burn ComputeInsertion(
        TransferParameters p, Vector3d vT2, Vector3d v2Body, double arrivalUT)
    {
        if (p.NoInsertionBurn)
            return new Burn(0.0, arrivalUT, BurnVector.Zero);

        return ManeuverComputer.Compute(new ManeuverParameters(
            p.DestinationOrbit, p.Destination, vT2, v2Body, false, arrivalUT));
    }

    /// <summary>
    /// Semi-major axis and eccentricity → periapsis and apoapsis distances [m]
    /// from the central body centre, derived from the departure state vector.
    /// </summary>
    private static (double Periapsis, double Apoapsis) TransferOrbitShape(
        Vector3d r, Vector3d v, double mu)
    {
        double rMag = r.Magnitude;
        double vMag = v.Magnitude;
        if (rMag < 1.0 || vMag < 1.0) return (0.0, 0.0);

        double a = 1.0 / (2.0 / rMag - vMag * vMag / mu);
        var    eccVec = (1.0 / mu) * ((vMag * vMag - mu / rMag) * r
                                      - Vector3d.Dot(r, v) * v);
        double e = eccVec.Magnitude;
        if (a <= 0.0) return (0.0, 0.0);
        return (a * (1.0 - e), a * (1.0 + e));
    }

    /// <summary>
    /// Phase angle: signed angular separation of destination from origin
    /// in the origin's orbital plane at departure.
    ///
    /// Positive when destination is ahead (prograde). Subtracts 2π for
    /// outer→inner transfers to give a negative "lead-in" angle, matching LWP.
    /// </summary>
    private static double ComputePhaseAngle(
        Vector3d r1, Vector3d r2, Vector3d v1Body,
        double originSma, double destSma)
    {
        var n0       = Vector3d.Cross(r1, v1Body).Normalize();
        var r2Proj   = r2 - n0 * Vector3d.Dot(r2, n0);   // project onto origin plane
        double r1Mag = r1.Magnitude;
        double r2Mag = r2Proj.Magnitude;
        if (r1Mag < 1.0 || r2Mag < 1.0) return 0.0;

        double cos   = Math.Clamp(Vector3d.Dot(r1, r2Proj) / (r1Mag * r2Mag), -1.0, 1.0);
        double angle = Math.Acos(cos);
        if (Vector3d.Dot(Vector3d.Cross(r1, r2Proj), n0) < 0.0)
            angle = 2.0 * Math.PI - angle;

        // Match LWP: outer→inner → subtract 2π so phase angle is negative.
        if (destSma < originSma)
            angle -= 2.0 * Math.PI;

        return angle;
    }

    /// <summary>
    /// Inclination of the insertion hyperbola relative to the ecliptic [rad].
    /// equals asin(vInf_z / |vInf|) where vInf = vT2 − v2Body.
    /// </summary>
    private static double ComputeInsertionInclination(Vector3d vT2, Vector3d v2Body)
    {
        var    vInf    = vT2 - v2Body;
        double vInfMag = vInf.Magnitude;
        if (vInfMag < 1e-9) return 0.0;
        return Math.Asin(Math.Clamp(vInf.Z / vInfMag, -1.0, 1.0));
    }
}

