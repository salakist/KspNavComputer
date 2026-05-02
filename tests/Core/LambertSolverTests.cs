using KspNavComputer.Core.Bodies;
using KspNavComputer.Core.Mechanics;
using Xunit;

namespace KspNavComputer.Core.Tests;

/// <summary>
/// Tests for LambertSolver.
///
/// NOTE: the 180° (anti-podal) case is a known mathematical singularity of the
/// universal-variable Lambert formulation — the Lagrange g coefficient is zero
/// for any exactly-opposite geometry. Tests therefore use a 90° quarter-orbit
/// geometry where the closed-form answer is simply the circular orbital speed.
///
/// Validation for a circular orbit quarter-turn:
///   r1 = (a, 0, 0),  r2 = (0, a, 0),  TOF = T/4
///   |v1| = |v2| = √(μ/a)  (the circular speed)
/// </summary>
public class LambertSolverTests
{
    private static readonly double MuKerbol = BodyDatabase.Kerbol.GravParam;
    private static readonly double AKerbin  = BodyDatabase.Kerbin.Orbit!.SemiMajorAxis;

    // Quarter-period for a circular orbit of radius a
    private static double QuarterPeriod(double a) =>
        0.5 * Math.PI * Math.Sqrt(a * a * a / MuKerbol);

    [Fact]
    public void CircularOrbit_QuarterTurn_DepartureSpeedEqualsCircularSpeed()
    {
        Vector3d r1 = new(AKerbin, 0, 0);
        Vector3d r2 = new(0, AKerbin, 0);
        double tof = QuarterPeriod(AKerbin);

        var (v1, _) = LambertSolver.Solve(r1, r2, tof, MuKerbol, prograde: true);

        double vCirc = Math.Sqrt(MuKerbol / AKerbin);
        Assert.Equal(vCirc, v1.Magnitude, precision: 1);  // within 0.05 m/s
    }

    [Fact]
    public void CircularOrbit_QuarterTurn_ArrivalSpeedEqualsCircularSpeed()
    {
        Vector3d r1 = new(AKerbin, 0, 0);
        Vector3d r2 = new(0, AKerbin, 0);
        double tof = QuarterPeriod(AKerbin);

        var (_, v2) = LambertSolver.Solve(r1, r2, tof, MuKerbol, prograde: true);

        double vCirc = Math.Sqrt(MuKerbol / AKerbin);
        Assert.Equal(vCirc, v2.Magnitude, precision: 1);
    }

    [Fact]
    public void CircularOrbit_ProgradeAndRetrograde_BothEqualCircularSpeed()
    {
        // Prograde = 90° arc (T/4), retrograde = 270° arc (3T/4).
        // Both are legs of the same circular orbit → |v1| = circular speed.
        Vector3d r1 = new(AKerbin, 0, 0);
        Vector3d r2 = new(0, AKerbin, 0);

        var (v1Pro, _) = LambertSolver.Solve(
            r1, r2, QuarterPeriod(AKerbin), MuKerbol, prograde: true);

        var (v1Retro, _) = LambertSolver.Solve(
            r1, r2, 3.0 * QuarterPeriod(AKerbin), MuKerbol, prograde: false);

        double vCirc = Math.Sqrt(MuKerbol / AKerbin);
        Assert.Equal(vCirc, v1Pro.Magnitude,   precision: 1);
        Assert.Equal(vCirc, v1Retro.Magnitude, precision: 1);
    }
}
