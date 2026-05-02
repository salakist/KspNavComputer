namespace KspNavComputer.Core.Mechanics;

/// <summary>
/// Lambert's problem solver using the universal-variable / Battin formulation.
/// Reference: Bate, Mueller &amp; White (1971); Izzo (2015) for the multi-rev case
/// (only 0-rev solutions are used in Increment 1).
///
/// Given two position vectors r1, r2 and a time-of-flight dt, solves for the
/// departure velocity v1 and arrival velocity v2 of the connecting conic.
/// </summary>
public static class LambertSolver
{
    private const double Tolerance    = 1e-6;
    private const int    MaxIter      = 500;

    // -------------------------------------------------------------------------
    // Stumpff functions
    // -------------------------------------------------------------------------

    private static double StumpffC(double psi)
    {
        if (psi > 1e-6)
        {
            double sqrtPsi = Math.Sqrt(psi);
            return (1.0 - Math.Cos(sqrtPsi)) / psi;
        }
        if (psi < -1e-6)
        {
            double sqrtNeg = Math.Sqrt(-psi);
            return (1.0 - Math.Cosh(sqrtNeg)) / psi;
        }
        return 0.5 - psi / 24.0 + psi * psi / 720.0;   // Taylor series around 0
    }

    private static double StumpffS(double psi)
    {
        if (psi > 1e-6)
        {
            double sqrtPsi = Math.Sqrt(psi);
            return (sqrtPsi - Math.Sin(sqrtPsi)) / (psi * sqrtPsi);
        }
        if (psi < -1e-6)
        {
            double sqrtNeg = Math.Sqrt(-psi);
            return (Math.Sinh(sqrtNeg) - sqrtNeg) / ((-psi) * sqrtNeg);
        }
        return 1.0 / 6.0 - psi / 120.0 + psi * psi / 5040.0;
    }

    // -------------------------------------------------------------------------
    // Public API
    // -------------------------------------------------------------------------

    /// <summary>
    /// Solves Lambert's problem for an elliptical (0-revolution) transfer.
    /// </summary>
    /// <param name="r1">Departure position vector [m] in inertial frame.</param>
    /// <param name="r2">Arrival position vector [m] in inertial frame.</param>
    /// <param name="dt">Time of flight [s]. Must be positive.</param>
    /// <param name="mu">Gravitational parameter of the central body [m³/s²].</param>
    /// <param name="prograde">
    ///   True → short-way transfer (transfer angle &lt; 180°).
    ///   False → long-way (retrograde) transfer.
    /// </param>
    /// <returns>
    ///   (v1) departure velocity [m/s], (v2) arrival velocity [m/s].
    /// </returns>
    /// <exception cref="InvalidOperationException">
    ///   Thrown when the solver fails to converge (degenerate geometry or
    ///   numerically problematic input).
    /// </exception>
    public static (Vector3d V1, Vector3d V2) Solve(
        Vector3d r1, Vector3d r2, double dt, double mu, bool prograde = true)
    {
        double r1Mag = r1.Magnitude;
        double r2Mag = r2.Magnitude;

        // Transfer angle Δν
        double cosDnu = Vector3d.Dot(r1, r2) / (r1Mag * r2Mag);
        cosDnu = Math.Clamp(cosDnu, -1.0, 1.0);

        // Determine transfer direction from cross-product z-component
        double crossZ = r1.X * r2.Y - r1.Y * r2.X;
        bool   shortWay = prograde ? crossZ >= 0 : crossZ < 0;

        double dnu = Math.Acos(cosDnu);
        if (!shortWay) dnu = 2.0 * Math.PI - dnu;

        // A parameter (Battin / Bate-Mueller-White §5.3)
        double A = Math.Sin(dnu) * Math.Sqrt(r1Mag * r2Mag / (1.0 - cosDnu));
        // When Δν = 0 or 2π the problem is degenerate (same point)
        if (Math.Abs(A) < 1e-6)
            throw new InvalidOperationException("Lambert: degenerate geometry (Δν ≈ 0 or 2π).");

        // Bisect / Newton-Raphson on F(ψ) = TOF(ψ) - dt = 0
        // Bounds: ψ ∈ (-4π², 4π²) for elliptic/hyperbolic; start in elliptic range
        double psiLow  = -4.0 * Math.PI * Math.PI;
        double psiHigh =  4.0 * Math.PI * Math.PI;
        double psi     = 0.0;
        double y       = 0.0, x = 0.0;

        for (int i = 0; i < MaxIter; i++)
        {
            double c2 = StumpffC(psi);
            double c3 = StumpffS(psi);

            y = r1Mag + r2Mag + A * (psi * c3 - 1.0) / Math.Sqrt(c2);
            if (y < 0) { psiLow = psi; psi = (psiLow + psiHigh) / 2.0; continue; }

            x   = Math.Sqrt(y / c2);
            double tof = (x * x * x * c3 + A * Math.Sqrt(y)) / Math.Sqrt(mu);

            if (tof < dt)
                psiLow  = psi;
            else
                psiHigh = psi;

            double psiNew = (psiLow + psiHigh) / 2.0;
            if (Math.Abs(psiNew - psi) < Tolerance) break;
            psi = psiNew;
        }

        // Reconstruct f, g, f_dot, g_dot Lagrange coefficients
        double f     = 1.0 - y / r1Mag;
        double g     = A * Math.Sqrt(y / mu);
        double gDot  = 1.0 - y / r2Mag;

        Vector3d v1 = (r2 - f * r1) / g;
        Vector3d v2 = (gDot * r2 - r1) / g;

        return (v1, v2);
    }
}
