using KspNavComputer.Core.Mechanics;
using KspNavComputer.Core.Maneuver;

namespace KspNavComputer.Core.Transfer;

/// <summary>
/// Computes mid-course plane-change transfers.
///
/// Ported from alexmoon's LWP <c>orbit.coffee</c> <c>Orbit.transfer("optimalPlaneChange")</c>
/// and <c>Orbit.transfer("planeChange", …, θ)</c>.
///
/// Algorithm:
///   1. Derive origin orbital-plane normal n₀ = normalize(r₁ × v₁).
///   2. Compute relative inclination Δi = asin(dot(r₂, n₀) / |r₂|).
///   3. Golden-section search over θ ∈ [0, π/2] or [π/2, π] (depending on
///      whether the destination orbit is higher or lower) to minimise the
///      plane-change Δv at true anomaly (νArrival − θ).
///   4. Run the golden-section search a second time on a refined orbit to
///      get the final angle-to-intercept θ*.
///   5. Compute the actual transfer with that θ*:
///        • Rotate r₂ by −planeChangeAngle around the plane-change axis.
///        • Solve Lambert for the rotated geometry (0-rev prograde only).
///        • Compute plane-change Δv and burn time on the resulting orbit.
///        • Rotate the insertion velocity back to the real frame.
/// </summary>
internal static class PlaneChangeComputer
{
    private const double HalfPi = Math.PI / 2.0;
    private const double TwoPi  = 2.0 * Math.PI;

    // Minimal state needed for the heliocentric transfer orbit.
    private sealed record TransferOrbitState(
        double   Sma,          // semi-major axis [m]
        double   Ecc,          // eccentricity
        double   Mu,           // gravitational parameter [m³/s²]
        double   TimePeriapsis,// time of periapsis passage [s UT]
        Vector3d EccVec,       // eccentricity vector (|EccVec| = Ecc, toward periapsis)
        Vector3d H             // specific angular momentum h = r × v
    );

    // -------------------------------------------------------------------------
    // Public entry point
    // -------------------------------------------------------------------------

    /// <summary>
    /// Computes the optimal mid-course plane-change transfer.
    /// Returns null when the plane change is not beneficial:
    ///   - transfer angle ≤ π/2, or
    ///   - relative inclination is effectively zero (coplanar bodies), or
    ///   - Lambert solver fails for the rotated geometry.
    /// </summary>
    public static PlaneChangeResult? Compute(PlaneChangeParameters p)
    {
        var n0 = Vector3d.Cross(p.R1, p.V1Body).Normalize();

        double relativeInclination = Math.Asin(
            Math.Clamp(Vector3d.Dot(p.R2, n0) / p.R2.Magnitude, -1.0, 1.0));
        if (Math.Abs(relativeInclination) < 1e-6)
            return null; // coplanar — no plane change useful

        double transferAngle = ComputeTransferAngle(p.R1, p.R2);
        if (transferAngle <= HalfPi)
            return null; // short transfer — plane change not beneficial

        // Search bounds: to lower orbit → [π/2, π]; to higher orbit → [0, π/2]
        double x1, x2;
        if (p.R2.Magnitude < p.R1.Magnitude) { x1 = HalfPi; x2 = Math.PI; }
        else                             { x1 = 0.0;    x2 = HalfPi;  }

        // ---- First approximation orbit ----
        // Rotate r2 by -relativeInclination around Cross(r2, n0) to flatten
        // it into the origin orbital plane.
        var approxAxis = Vector3d.Cross(p.R2, n0).Normalize();
        var r2Approx   = RotateByAxisAngle(p.R2, approxAxis, -relativeInclination);

        var lambert0 = LambertSolver.SolveAllRevolutions(
            p.R1, r2Approx, p.TimeOfFlight, p.Mu, maxRevs: 0, prograde: true);
        if (lambert0.Count == 0) return null;

        var approxOrbit    = OrbitFromState(p.R1, lambert0[0].V1, p.Mu, p.DepartureUT);
        double approxNuArr = TrueAnomalyAtPosition(approxOrbit, r2Approx);

        double xOpt = GoldenSectionSearch(x1, x2, 1e-2, x =>
        {
            double angle = Math.Atan2(Math.Tan(relativeInclination), Math.Sin(x));
            return Math.Abs(2.0 * SpeedAtTrueAnomaly(approxOrbit, approxNuArr - x)
                               * Math.Sin(0.5 * angle));
        });

        // ---- Refinement: build a better orbit with the new axis ----
        double pcAngleRef = Math.Atan2(Math.Tan(relativeInclination), Math.Sin(xOpt));
        var    pcAxisRef  = RotateByAxisAngle(
                                ProjectToPlane(p.R2, n0).Normalize(), n0, -xOpt);
        // LWP uses FORWARD rotation in the refinement step (see orbit.coffee)
        var r2Refined = RotateByAxisAngle(p.R2, pcAxisRef, pcAngleRef);

        var lambert1 = LambertSolver.SolveAllRevolutions(
            p.R1, r2Refined, p.TimeOfFlight, p.Mu, maxRevs: 0, prograde: true);
        if (lambert1.Count == 0) return null;

        var refinedOrbit    = OrbitFromState(p.R1, lambert1[0].V1, p.Mu, p.DepartureUT);
        double refinedNuArr = TrueAnomalyAtPosition(refinedOrbit, r2Refined);

        xOpt = GoldenSectionSearch(x1, x2, 1e-2, x =>
        {
            double angle = Math.Atan2(Math.Tan(relativeInclination), Math.Sin(x));
            return Math.Abs(2.0 * SpeedAtTrueAnomaly(refinedOrbit, refinedNuArr - x)
                               * Math.Sin(0.5 * angle));
        });

        // ---- Final transfer with optimal θ ----
        return ComputeWithAngle(p.R1, p.R2, n0, p.TimeOfFlight, p.DepartureUT, p.Mu,
                                xOpt, relativeInclination);
    }

    // -------------------------------------------------------------------------
    // Transfer angle helper (used by TransferComputer as well)
    // -------------------------------------------------------------------------

    /// <summary>
    /// Counter-clockwise transfer angle from r1 to r2 in [0, 2π].
    /// </summary>
    internal static double ComputeTransferAngle(Vector3d r1, Vector3d r2)
    {
        double cos   = Math.Clamp(Vector3d.Dot(r1.Normalize(), r2.Normalize()), -1.0, 1.0);
        double angle = Math.Acos(cos);
        // Clockwise in XY plane → angle > 180°  (matches LWP: (p0×p1).z < 0)
        if (r1.X * r2.Y - r1.Y * r2.X < 0.0)
            angle = TwoPi - angle;
        return angle;
    }

    // -------------------------------------------------------------------------
    // Core plane-change transfer for a fixed θ
    // -------------------------------------------------------------------------

    private static PlaneChangeResult?
        ComputeWithAngle(
            Vector3d r1, Vector3d r2,
            Vector3d n0,
            double tof, double t0, double mu,
            double x, double relativeInclination)
    {
        double planeChangeAngle = Math.Atan2(Math.Tan(relativeInclination), Math.Sin(x));
        if (Math.Abs(planeChangeAngle) < 1e-9)
            return null;

        double transferAngle = ComputeTransferAngle(r1, r2);
        if (transferAngle <= HalfPi)
            return null;

        // Plane-change axis: project r2 onto origin plane, then rotate by -θ around n0.
        var pcAxis = RotateByAxisAngle(ProjectToPlane(r2, n0).Normalize(), n0, -x);

        // Rotate r2 into origin's orbital plane (conjugate = −planeChangeAngle).
        // This matches LWP's: p1InOriginPlane = rotate(conjugate(rotation), p1).
        var r2InOriginPlane = RotateByAxisAngle(r2, pcAxis, -planeChangeAngle);

        // Lambert solve — 0-rev prograde only, matching LWP.
        var solution = LambertSolver.SolveAllRevolutions(
            r1, r2InOriginPlane, tof, mu, maxRevs: 0, prograde: true);
        if (solution.Count == 0) return null;

        var (vT1, vT2InOriginPlane) = solution[0];

        // Build transfer orbit from departure state.
        double t1    = t0 + tof;
        var orbit    = OrbitFromState(r1, vT1, mu, t0);

        // Plane-change burn parameters.
        double pcTrueAnomaly   = TrueAnomalyAt(orbit, t1) - x;
        double speed           = SpeedAtTrueAnomaly(orbit, pcTrueAnomaly);
        double planeChangeDv   = double.IsNaN(speed) || double.IsInfinity(speed)
                                    ? 0.0
                                    : Math.Abs(2.0 * speed * Math.Sin(planeChangeAngle / 2.0));
        double planeChangeTime = TimeAtTrueAnomaly(orbit, pcTrueAnomaly, t0);

        // Burn vector (from LWP porkchop.coffee lines 75-77).
        double prograde = -planeChangeDv * Math.Abs(Math.Sin(planeChangeAngle / 2.0));
        double normal   =  planeChangeDv * Math.Sign(planeChangeAngle)
                                         * Math.Cos(planeChangeAngle / 2.0);

        var pcBurn = new Burn(
            DeltaV: planeChangeDv,
            BurnUT: planeChangeTime,
            Vector: new BurnVector(prograde, normal, 0.0));

        // Rotate insertion velocity back to real space (forward rotation).
        var vT2 = RotateByAxisAngle(vT2InOriginPlane, pcAxis, planeChangeAngle);

        return new PlaneChangeResult(vT1, vT2, pcBurn);
    }

    // -------------------------------------------------------------------------
    // Orbit from position and velocity (port of LWP Orbit.fromPositionAndVelocity)
    // -------------------------------------------------------------------------

    private static TransferOrbitState OrbitFromState(
        Vector3d r, Vector3d v, double mu, double t)
    {
        double rMag = r.Magnitude;
        double vMag = v.Magnitude;

        var    h      = Vector3d.Cross(r, v);
        var    eccVec = (1.0 / mu) * ((vMag * vMag - mu / rMag) * r
                                      - Vector3d.Dot(r, v) * v);
        double e = eccVec.Magnitude;
        double a = 1.0 / (2.0 / rMag - vMag * vMag / mu);

        // True anomaly at departure.
        double cosNu = e > 1e-10
            ? Math.Clamp(Vector3d.Dot(eccVec, r) / (e * rMag), -1.0, 1.0)
            : 0.0;
        double nu = Math.Acos(cosNu);
        if (Vector3d.Dot(r, v) < 0.0) nu = -nu;

        // Mean anomaly → time of periapsis.
        double E = 2.0 * Math.Atan(Math.Tan(nu / 2.0)
                       * Math.Sqrt((1.0 - e) / (1.0 + e)));
        if (E < 0.0) E += TwoPi;
        double M          = E - e * Math.Sin(E);
        double meanMotion = Math.Sqrt(mu / Math.Abs(a * a * a));
        double tPeriapsis = t - M / meanMotion;

        return new(a, e, mu, tPeriapsis, eccVec, h);
    }

    // -------------------------------------------------------------------------
    // Transfer-orbit helpers
    // -------------------------------------------------------------------------

    private static double SpeedAtTrueAnomaly(TransferOrbitState orbit, double nu)
    {
        double e          = orbit.Ecc;
        double semiLatus  = orbit.Sma * (1.0 - e * e);
        double r          = semiLatus / (1.0 + e * Math.Cos(nu));
        if (r <= 0.0 || orbit.Sma <= 0.0) return double.NaN;
        return Math.Sqrt(orbit.Mu * (2.0 / r - 1.0 / orbit.Sma));
    }

    private static double TrueAnomalyAt(TransferOrbitState orbit, double t)
    {
        double n = Math.Sqrt(orbit.Mu / Math.Abs(orbit.Sma * orbit.Sma * orbit.Sma));
        double M = n * (t - orbit.TimePeriapsis);
        M %= TwoPi;
        if (M < 0.0) M += TwoPi;
        double E = KeplerSolver.SolveEccentricAnomaly(M, orbit.Ecc);
        return KeplerSolver.TrueAnomalyFromEccentric(E, orbit.Ecc);
    }

    /// <summary>
    /// True anomaly of a position in the transfer orbit's perifocal frame.
    /// Returns a value in [−π, π].
    /// </summary>
    private static double TrueAnomalyAtPosition(TransferOrbitState orbit, Vector3d p)
    {
        var xPeri = orbit.EccVec.Normalize();
        var zPeri = orbit.H.Normalize();
        var yPeri = Vector3d.Cross(zPeri, xPeri);
        return Math.Atan2(Vector3d.Dot(p, yPeri), Vector3d.Dot(p, xPeri));
    }

    /// <summary>
    /// Next time after t0 when the transfer orbit reaches true anomaly nu.
    /// </summary>
    private static double TimeAtTrueAnomaly(TransferOrbitState orbit, double nu, double t0)
    {
        double e = orbit.Ecc;
        double E = 2.0 * Math.Atan(Math.Tan(nu / 2.0)
                       * Math.Sqrt((1.0 - e) / (1.0 + e)));
        double M          = E - e * Math.Sin(E);
        double meanMotion = Math.Sqrt(orbit.Mu / Math.Abs(orbit.Sma * orbit.Sma * orbit.Sma));
        double period     = TwoPi / meanMotion;
        double t          = orbit.TimePeriapsis
                          + period * Math.Floor((t0 - orbit.TimePeriapsis) / period)
                          + M / meanMotion;
        if (t < t0) t += period;
        return t;
    }

    // -------------------------------------------------------------------------
    // Vector geometry helpers
    // -------------------------------------------------------------------------

    /// <summary>Rodrigues' rotation formula — axis must be a unit vector.</summary>
    private static Vector3d RotateByAxisAngle(Vector3d v, Vector3d axis, double angle)
    {
        double cos = Math.Cos(angle);
        double sin = Math.Sin(angle);
        return v * cos
             + Vector3d.Cross(axis, v) * sin
             + axis * Vector3d.Dot(axis, v) * (1.0 - cos);
    }

    private static Vector3d ProjectToPlane(Vector3d p, Vector3d n)
        => p - n * Vector3d.Dot(p, n);

    // -------------------------------------------------------------------------
    // Golden-section minimisation
    // -------------------------------------------------------------------------

    private static double GoldenSectionSearch(
        double a, double b, double tol, Func<double, double> f)
    {
        const double Gr = 0.6180339887498949; // golden ratio conjugate
        double c = b - Gr * (b - a);
        double d = a + Gr * (b - a);
        while (Math.Abs(c - d) > tol)
        {
            if (f(c) < f(d)) { b = d; d = c; c = b - Gr * (b - a); }
            else              { a = c; c = d; d = a + Gr * (b - a); }
        }
        return (a + b) / 2.0;
    }
}
