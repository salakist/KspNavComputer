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

// -------------------------------------------------------------------------
// Maneuver Δv calculator (isolated to keep TransferComputer focused on
// orchestration; not a separate file as the method is a private detail)
// -------------------------------------------------------------------------

/// <summary>
/// Converts a heliocentric transfer velocity vector into a <see cref="Burn"/>:
/// Δv scalar, precise periapsis burn UT, and prograde/normal/radial components.
///
/// Burn vector decomposition (periapsis frame, all in m/s):
///   Ejection:  prograde = v_hyper·cos(ΔI) − v_park
///              normal   = v_hyper·sin(ΔI)·sign(α)
///   Insertion: prograde = v_park − v_hyper·cos(ΔI_arr)  (retrograde)
///              normal   = −v_hyper·sin(ΔI_arr)·sign(α_arr)
///   Radial = 0 in both cases.
///
/// Inclination treatment:
///   Ejection:  ΔI = max(0, |α| − i_park), where α = asin(v∞_z / |v∞|).
///   Insertion: ΔI_arr = max(0, |α_arr| − i_dest) when i_dest > 0.
///              When i_dest = 0, insertion is pure deceleration
///              ("capture into natural arrival plane", backward compatible).
///
/// Periapsis burn UT:
///   |a| = μ/v∞²,  e = 1 + r_peri/|a|
///   F = acosh((r_SOI/|a| + 1)/e),  t = √(|a|³/μ)·(e·sinh(F) − F)
///   Ejection:  burnUT = refUT − t  (burn before SOI exit)
///   Insertion: burnUT = refUT + t  (burn after SOI entry)
/// </summary>
internal static class ManeuverCalculator
{
    internal static Burn Compute(
        ParkingOrbit parkingOrbit, CelestialBody body,
        Vector3d vTransfer, Vector3d vBody, bool isEjection, double refUT)
    {
        double muBody = body.GravParam;
        double rPeri  = body.Radius + parkingOrbit.Altitude;
        double rSoi   = body.SphereOfInfluence;
        double e      = parkingOrbit.Eccentricity;

        // Parking orbit periapsis speed: √(μ·(1+e)/r). Equals v_circ when e=0.
        double vPark = Math.Sqrt(muBody * (1.0 + e) / rPeri);

        // Hyperbolic excess velocity vector and periapsis speed.
        Vector3d vInfVec    = vTransfer - vBody;
        double   vInf       = vInfVec.Magnitude;
        double   vHyperPeri = Math.Sqrt(vInf * vInf + 2.0 * muBody / rPeri - 2.0 * muBody / rSoi);

        // ---- Precise periapsis burn UT via hyperbolic transit time ----
        // |a| = μ/v∞²,  e_hyp = 1 + r_peri/|a|
        // F_SOI = acosh((r_SOI/|a| + 1) / e_hyp)
        // t_transit = √(|a|³/μ) · (e_hyp·sinh(F_SOI) − F_SOI)
        // Ejection:  burn is before SOI exit  → burnUT = refUT − t_transit
        // Insertion: burn is after SOI entry  → burnUT = refUT + t_transit
        double burnUT;
        if (vInf < 1e-9)
        {
            burnUT = refUT; // degenerate: no meaningful hyperbolic arc
        }
        else
        {
            double sma     = muBody / (vInf * vInf);
            double eHyp    = 1.0 + rPeri / sma;
            double cosHF   = (rSoi / sma + 1.0) / eHyp;
            double hfSoi   = Math.Acosh(cosHF);
            double transit = Math.Sqrt(sma * sma * sma / muBody) * (eHyp * Math.Sinh(hfSoi) - hfSoi);
            burnUT = isEjection ? refUT - transit : refUT + transit;
        }

        // ---- Ejection: combined speed-up + plane-change at periapsis ----
        if (isEjection)
        {
            if (Math.Abs(vInfVec.Z) < 1e-9 || vInf < 1e-9)
            {
                double dv0 = vHyperPeri - vPark;
                return new Burn(Math.Abs(dv0), burnUT, new BurnVector(dv0, 0.0, 0.0));
            }

            double alpha  = Math.Asin(Math.Clamp(vInfVec.Z / vInf, -1.0, 1.0));
            double deltaI = Math.Max(0.0, Math.Abs(alpha) - parkingOrbit.Inclination);

            if (deltaI < 1e-9)
            {
                double dv0 = vHyperPeri - vPark;
                return new Burn(Math.Abs(dv0), burnUT, new BurnVector(dv0, 0.0, 0.0));
            }

            double proEj = vHyperPeri * Math.Cos(deltaI) - vPark;
            double norEj = vHyperPeri * Math.Sin(deltaI) * (double)Math.Sign(alpha);
            double dvEj  = Math.Sqrt(proEj * proEj + norEj * norEj);
            return new Burn(dvEj, burnUT, new BurnVector(proEj, norEj, 0.0));
        }

        // ---- Insertion: deceleration + optional plane-change into destination orbit ----
        // i_dest = 0 → pure deceleration ("capture into natural arrival plane").
        double iDest = parkingOrbit.Inclination;
        if (iDest < 1e-9 || Math.Abs(vInfVec.Z) < 1e-9 || vInf < 1e-9)
        {
            double dv0 = vPark - vHyperPeri; // negative = retrograde
            return new Burn(Math.Abs(dv0), burnUT, new BurnVector(dv0, 0.0, 0.0));
        }

        double alphaArr  = Math.Asin(Math.Clamp(vInfVec.Z / vInf, -1.0, 1.0));
        double deltaIArr = Math.Max(0.0, Math.Abs(alphaArr) - iDest);

        if (deltaIArr < 1e-9)
        {
            double dv0 = vPark - vHyperPeri;
            return new Burn(Math.Abs(dv0), burnUT, new BurnVector(dv0, 0.0, 0.0));
        }

        double proIns = vPark - vHyperPeri * Math.Cos(deltaIArr);
        double norIns = -vHyperPeri * Math.Sin(deltaIArr) * (double)Math.Sign(alphaArr);
        double dvIns  = Math.Sqrt(proIns * proIns + norIns * norIns);
        return new Burn(dvIns, burnUT, new BurnVector(proIns, norIns, 0.0));
    }
}
