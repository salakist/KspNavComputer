namespace KspNavComputer.Core.Mechanics;

/// <summary>
/// Lambert's problem solver using the Sun (1979) / Gooding formulation.
/// Supports multi-revolution solutions, matching alexmoon's Launch Window Planner (LWP).
/// </summary>
public static class LambertSolver
{
    private const double MachineEps = 2.22e-16;
    private const double RootTol    = 1e-6;

    // -------------------------------------------------------------------------
    // Inverse hyperbolic cotangent
    // -------------------------------------------------------------------------
    private static double Acoth(double x) => 0.5 * Math.Log((x + 1.0) / (x - 1.0));

    // -------------------------------------------------------------------------
    // Inverse cotangent
    // -------------------------------------------------------------------------
    private static double Acot(double x) => 0.5 * Math.PI - Math.Atan(x);

    // -------------------------------------------------------------------------
    // Brent's root-finding method
    // -------------------------------------------------------------------------
    private static double Brent(double a, double b, double tol, Func<double, double> f)
    {
        double fa = f(a), fb = f(b);
        double c = a, fc = fa, d = 0, e = 0;
        for (int i = 0; i < 500; i++)
        {
            if (fb * fc > 0) { c = a; fc = fa; d = e = b - a; }
            if (Math.Abs(fc) < Math.Abs(fb)) { a = b; b = c; c = a; fa = fb; fb = fc; fc = fa; }
            double tol1 = 2.0 * MachineEps * Math.Abs(b) + 0.5 * tol;
            double xm   = 0.5 * (c - b);
            if (Math.Abs(xm) <= tol1 || fb == 0) return b;
            if (Math.Abs(e) >= tol1 && Math.Abs(fa) > Math.Abs(fb))
            {
                double s = fb / fa, p, q, r2;
                if (a == c) { p = 2.0 * xm * s; q = 1.0 - s; }
                else
                {
                    q = fa / fc; r2 = fb / fc;
                    p = s * (2.0 * xm * q * (q - r2) - (b - a) * (r2 - 1.0));
                    q = (q - 1.0) * (r2 - 1.0) * (s - 1.0);
                }
                if (p > 0) q = -q; else p = -p;
                if (2.0 * p < Math.Min(3.0 * xm * q - Math.Abs(tol1 * q), Math.Abs(e * q)))
                { e = d; d = p / q; }
                else { d = xm; e = d; }
            }
            else { d = xm; e = d; }
            a = b; fa = fb;
            b += Math.Abs(d) > tol1 ? d : (xm > 0 ? tol1 : -tol1);
            fb = f(b);
        }
        return b;
    }

    // -------------------------------------------------------------------------
    // Public API
    // -------------------------------------------------------------------------

    /// <summary>
    /// Solves Lambert's problem and returns all solutions across revolutions 0..maxRevs.
    /// Matches LWP's lambert() with the same prograde convention:
    ///   prograde=1  → short-way (transferAngle &lt; 180°)
    ///   prograde=-1 → long-way  (transferAngle &gt; 180°)
    /// </summary>
    public static List<(Vector3d V1, Vector3d V2)> SolveAllRevolutions(
        Vector3d r1, Vector3d r2, double dt, double mu, int maxRevs = 10, bool prograde = true)
    {
        double r1Mag = r1.Magnitude;
        double r2Mag = r2.Magnitude;

        Vector3d deltaPos = r2 - r1;
        double c = deltaPos.Magnitude;
        double m = r1Mag + r2Mag + c;
        double n = r1Mag + r2Mag - c;

        // LWP: transferAngle > PI → angleParameter negative
        // prograde=true → cross*1 >= 0 means short-way; cross*-1 means short-way reversed
        double crossZ        = r1.X * r2.Y - r1.Y * r2.X;
        int   progradeSign   = prograde ? 1 : -1;
        double cosTA         = Vector3d.Dot(r1, r2) / (r1Mag * r2Mag);
        cosTA = Math.Clamp(cosTA, -1.0, 1.0);
        double transferAngle = Math.Acos(cosTA);
        if (crossZ * progradeSign < 0)
            transferAngle = 2 * Math.PI - transferAngle;

        double angleParameter = Math.Sqrt(n / m);
        if (transferAngle > Math.PI)
            angleParameter = -angleParameter;

        double normalizedTime          = 4.0 * dt * Math.Sqrt(mu / (m * m * m));
        double parabolicNormalizedTime = 2.0 / 3.0 * (1.0 - angleParameter * angleParameter * angleParameter);

        double sqrtMu   = Math.Sqrt(mu);
        double invSqrtM = 1.0 / Math.Sqrt(m);
        double invSqrtN = 1.0 / Math.Sqrt(n);

        var solutions = new List<(Vector3d, Vector3d)>();

        double Fy(double xv)
        {
            double sq = Math.Sqrt(Math.Max(0, 1.0 - angleParameter * angleParameter * (1.0 - xv * xv)));
            return angleParameter < 0 ? -sq : sq;
        }

        void PushSolution(double xv, double yv)
        {
            double vc = sqrtMu * (yv * invSqrtN + xv * invSqrtM);
            double vr = sqrtMu * (yv * invSqrtN - xv * invSqrtM);
            Vector3d ec = deltaPos * (vc / c);
            solutions.Add((ec + r1 * (vr / r1Mag), ec - r2 * (vr / r2Mag)));
        }

        if (RelErr(normalizedTime, parabolicNormalizedTime) < 1e-6)
        {
            double yv = angleParameter < 0 ? -1.0 : 1.0;
            PushSolution(1.0, yv);
            return solutions;
        }

        if (normalizedTime < parabolicNormalizedTime)
        {
            // Hyperbolic
            double FdtH(double xv)
            {
                double yv = Fy(xv);
                double g  = Math.Sqrt(xv * xv - 1.0);
                double h  = Math.Sqrt(yv * yv - 1.0);
                return (Acoth(yv / h) - Acoth(xv / g) + xv * g - yv * h) / (g * g * g) - normalizedTime;
            }
            double lo = 1.0 + MachineEps, hi = 2.0;
            while (FdtH(hi) > 0.0) { lo = hi; hi *= 2.0; }
            double xv2 = Brent(lo, hi, RootTol, FdtH);
            PushSolution(xv2, Fy(xv2));
            return solutions;
        }

        // Elliptic — iterate 0..maxRevs revolutions
        double minimumEnergyTau = Math.Acos(angleParameter)
                                + angleParameter * Math.Sqrt(1.0 - angleParameter * angleParameter);
        int maxRevsCapped = Math.Min(maxRevs, (int)Math.Floor(normalizedTime / Math.PI));

        double FdtE(double xv, int Nrev)
        {
            double yv = Fy(xv);
            double g  = Math.Sqrt(Math.Max(0, 1.0 - xv * xv));
            double h  = Math.Sqrt(Math.Max(0, 1.0 - yv * yv));
            return (Acot(xv / g) - Math.Atan(h / yv) - xv * g + yv * h + Nrev * Math.PI) / (g * g * g) - normalizedTime;
        }

        double Phix(double xv)
        {
            double g = Math.Sqrt(1.0 - xv * xv);
            return Acot(xv / g) - (2.0 + xv * xv) * g / (3.0 * xv);
        }

        double Phiy(double yv)
        {
            double h = Math.Sqrt(1.0 - yv * yv);
            return Math.Atan(h / yv) - (2.0 + yv * yv) * h / (3.0 * yv);
        }

        for (int N = 0; N <= maxRevsCapped; N++)
        {
            if (N > 0 && N == maxRevsCapped)
            {
                // Find minimum-time x for this revolution count
                double xMT, minTau;
                if (angleParameter == 1.0)
                {
                    xMT    = 0.0;
                    minTau = minimumEnergyTau;
                }
                else if (angleParameter == 0.0)
                {
                    xMT    = Brent(0, 1, 1e-6, xv => Phix(xv) + N * Math.PI);
                    minTau = 2.0 / (3.0 * xMT);
                }
                else
                {
                    xMT    = Brent(0, 1, 1e-6, xv => Phix(xv) - Phiy(Fy(xv)) + N * Math.PI);
                    double yMT = Fy(xMT);
                    minTau = 2.0 / 3.0 * (1.0 / xMT - angleParameter * angleParameter * angleParameter / Math.Abs(yMT));
                }

                if (RelErr(normalizedTime, minTau) < 1e-6)
                {
                    PushSolution(xMT, Fy(xMT));
                    break;
                }
                if (normalizedTime < minTau) break;

                if (normalizedTime < minimumEnergyTau)
                {
                    double xA = Brent(0, xMT, 1e-4, xv => FdtE(xv, N));
                    if (!double.IsNaN(xA)) PushSolution(xA, Fy(xA));
                    double xB = Brent(xMT, 1.0 - MachineEps, 1e-4, xv => FdtE(xv, N));
                    if (!double.IsNaN(xB)) PushSolution(xB, Fy(xB));
                    break;
                }
            }

            if (RelErr(normalizedTime, minimumEnergyTau) < 1e-6)
            {
                PushSolution(0.0, Fy(0.0));
                if (N > 0)
                {
                    double xB = Brent(1e-6, 1.0 - MachineEps, 1e-4, xv => FdtE(xv, N));
                    if (!double.IsNaN(xB)) PushSolution(xB, Fy(xB));
                }
            }
            else
            {
                double brentTol = N == 0 ? RootTol : 1e-4;
                if (N > 0 || normalizedTime > minimumEnergyTau)
                {
                    double xA = Brent(-1.0 + MachineEps, 0, brentTol, xv => FdtE(xv, N));
                    if (!double.IsNaN(xA)) PushSolution(xA, Fy(xA));
                }
                if (N > 0 || normalizedTime < minimumEnergyTau)
                {
                    double xB = Brent(0, 1.0 - MachineEps, brentTol, xv => FdtE(xv, N));
                    if (!double.IsNaN(xB)) PushSolution(xB, Fy(xB));
                }
            }
        }

        return solutions;
    }

    /// <summary>
    /// Convenience single-solution wrapper. Returns the 0-revolution prograde or retrograde arc.
    /// </summary>
    public static (Vector3d V1, Vector3d V2) Solve(
        Vector3d r1, Vector3d r2, double dt, double mu, bool prograde = true)
    {
        var sols = SolveAllRevolutions(r1, r2, dt, mu, maxRevs: 0, prograde: prograde);
        if (sols.Count == 0) throw new InvalidOperationException("Lambert solver found no solution.");
        return sols[0];
    }

    private static double RelErr(double a, double b) => Math.Abs(1.0 - a / b);
}
