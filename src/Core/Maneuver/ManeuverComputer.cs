using KspNavComputer.Core.Bodies;
using KspNavComputer.Core.Mechanics;

namespace KspNavComputer.Core.Maneuver;

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
internal static class ManeuverComputer
{
    internal static Burn Compute(ManeuverParameters p)
    {
        ParkingOrbit parkingOrbit = p.ParkingOrbit;
        CelestialBody body        = p.Body;
        Vector3d vTransfer        = p.TransferVelocity;
        Vector3d vBody            = p.BodyVelocity;
        bool isEjection           = p.IsEjection;
        double refUT              = p.RefUT;

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
            EjectionDetails? ejDetails = ComputeEjectionDetails(vInfVec, rPeri, body, vBody);

            if (Math.Abs(vInfVec.Z) < 1e-9 || vInf < 1e-9)
            {
                double dv0 = vHyperPeri - vPark;
                return new Burn(Math.Abs(dv0), burnUT, new BurnVector(dv0, 0.0, 0.0), ejDetails);
            }

            double alpha  = Math.Asin(Math.Clamp(vInfVec.Z / vInf, -1.0, 1.0));
            double deltaI = Math.Max(0.0, Math.Abs(alpha) - parkingOrbit.Inclination);

            if (deltaI < 1e-9)
            {
                double dv0 = vHyperPeri - vPark;
                return new Burn(Math.Abs(dv0), burnUT, new BurnVector(dv0, 0.0, 0.0), ejDetails);
            }

            double proEj = vHyperPeri * Math.Cos(deltaI) - vPark;
            double norEj = vHyperPeri * Math.Sin(deltaI) * (double)Math.Sign(alpha);
            double dvEj  = Math.Sqrt(proEj * proEj + norEj * norEj);
            return new Burn(dvEj, burnUT, new BurnVector(proEj, norEj, 0.0), ejDetails);
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

    // -------------------------------------------------------------------------
    // Ejection angle and inclination (ported from LWP orbit.coffee)
    // -------------------------------------------------------------------------

    /// <summary>
    /// Computes ejection angle (signed, degrees) and escape-hyperbola inclination
    /// (degrees) for an ejection burn.
    ///
    /// Algorithm from LWP (alexmoon/ksp-launch-window-planner, orbit.coffee):
    ///   1. Compute hyperbola geometry (e, a) from v∞ and periapsis radius.
    ///   2. Find true anomaly at SOI exit plus zenith correction → θ.
    ///   3. Solve for the periapsis direction (unit vector in body's equatorial plane).
    ///   4. Ejection inclination = acos(z of orbital-plane normal).
    ///   5. Ejection angle = signed angle from periapsis to body prograde (XY projection).
    ///
    /// Returns null when the geometry is degenerate (zero v∞, no equatorial
    /// periapsis solution, or numerical failure).
    /// </summary>
    private static EjectionDetails? ComputeEjectionDetails(
        Vector3d vInfVec, double rPeri, CelestialBody body, Vector3d vBody)
    {
        double vInf = vInfVec.Magnitude;
        if (vInf < 1e-9) return null;

        double mu   = body.GravParam;
        double rSoi = body.SphereOfInfluence;

        // Speed at periapsis of escape hyperbola (same as vHyperPeri in Compute)
        double v1 = Math.Sqrt(vInf * vInf + 2.0 * mu / rPeri - 2.0 * mu / rSoi);

        // Hyperbola elements (a < 0 convention: a = r_peri / (1 − e), e > 1)
        double e = rPeri * v1 * v1 / mu - 1.0;   // eccentricity > 1
        // Semi-latus rectum p = rPeri*(1+e); orbit equation: r = p/(1 + e·cosθ)
        // At SOI: rSoi = rPeri*(1+e)/(1 + e·cosθ) → cosθ = (rPeri*(1+e)/rSoi − 1)/e
        double cosTheta = (rPeri * (1.0 + e) - rSoi) / (e * rSoi);
        cosTheta = Math.Clamp(cosTheta, -1.0, 1.0);
        double theta = Math.Acos(cosTheta);

        // Zenith angle correction (LWP eq 4.23): θ += asin(v1·r0 / (v∞·rSoi))
        double sinZenith = v1 * rPeri / (vInf * rSoi);
        if (Math.Abs(sinZenith) > 1.0) return null;
        theta += Math.Asin(sinZenith);

        // Normalized v∞ direction
        double ejX = vInfVec.X / vInf;
        double ejY = vInfVec.Y / vInf;
        double ejZ = vInfVec.Z / vInf;

        // Equatorial periapsis is possible only when |sin θ| ≥ |ejZ|
        double sinTheta = Math.Sin(theta);
        if (Math.Abs(sinTheta) < Math.Abs(ejZ)) return null;

        // ---- Find periapsis direction (unit vector in z=0 plane) ----
        // Constraints: dot(p, ejDir) = cos θ, p.z = 0, |p| = 1
        double cT = Math.Cos(theta);
        double pX, pY;

        if (Math.Abs(ejY) < 1e-9)
        {
            if (Math.Abs(ejX) < 1e-9) return null;
            pX = cT / ejX;
            if (Math.Abs(pX) > 1.0) return null;
            pY = Math.Sqrt(1.0 - pX * pX);
            // Choose CCW orbital direction: cross(p, ejDir).z = pX·ejY − pY·ejX > 0
            if (pX * ejY - pY * ejX < 0) pY = -pY;
        }
        else
        {
            // Solve quadratic (LWP: ejectionPeriapsisDirection)
            double g  = -ejX / ejY;
            double ac = 1.0 + g * g;
            double bc = 2.0 * g * cT / ejY;
            double cc = cT * cT / (ejY * ejY) - 1.0;

            double disc = bc * bc - 4.0 * ac * cc;
            if (disc < 0.0) return null;

            double q = bc < 0.0
                ? -0.5 * (bc - Math.Sqrt(disc))
                : -0.5 * (bc + Math.Sqrt(disc));

            if (Math.Abs(q) < 1e-15) return null;

            pX = q / ac;
            pY = g * pX + cT / ejY;

            // Ensure CCW orbital direction
            if (pX * ejY - pY * ejX < 0)
            {
                pX = cc / q;
                pY = g * pX + cT / ejY;
            }
        }

        // ---- Ejection inclination ----
        // orbital-plane normal = normalize(cross(periapsis, ejDir))
        // cross((pX,pY,0), (ejX,ejY,ejZ)) = (pY·ejZ, −pX·ejZ, pX·ejY−pY·ejX)
        double nX   = pY * ejZ;
        double nY   = -pX * ejZ;
        double nZ   = pX * ejY - pY * ejX;
        double nMag = Math.Sqrt(nX * nX + nY * nY + nZ * nZ);
        if (nMag < 1e-15) return null;

        double incRad = Math.Acos(Math.Clamp(nZ / nMag, -1.0, 1.0));
        // Sign: positive = north-tilted, negative = south-tilted (matches LWP)
        incRad *= Math.Sign(Math.PI - theta) * Math.Sign(ejZ);

        // ---- Ejection angle to prograde ----
        // Project body prograde onto XY plane (body's orbital plane)
        double progMag = Math.Sqrt(vBody.X * vBody.X + vBody.Y * vBody.Y);
        if (progMag < 1e-9) return null;
        double progX = vBody.X / progMag;
        double progY = vBody.Y / progMag;

        double dot     = Math.Clamp(pX * progX + pY * progY, -1.0, 1.0);
        double angleRad = Math.Acos(dot);

        // cross(periapsis, prograde).z determines orientation
        if (pX * progY - pY * progX < 0)
            angleRad = 2.0 * Math.PI - angleRad;

        // Map to signed convention: (0°, 180°] = prograde (+), (180°, 360°) → retrograde (−)
        double angleDeg = angleRad * 180.0 / Math.PI;
        if (angleDeg > 180.0) angleDeg = 180.0 - angleDeg; // now negative for retrograde

        return new EjectionDetails(angleDeg, incRad * 180.0 / Math.PI);
    }
}
