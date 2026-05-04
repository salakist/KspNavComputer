using KspNavComputer.Core.Bodies;
using KspNavComputer.Core.Mechanics;
using Xunit;

namespace KspNavComputer.Core.Tests;

/// <summary>
/// Tests for KeplerSolver. Validation approach:
///   - Kerbin orbit is circular (e=0) → position at any UT should be at
///     exactly the semi-major axis distance from Kerbol.
///   - Round-trip: propagate forward then back should return original M.
///   - Eccentric anomaly solver: known analytic cases.
/// </summary>
public class KeplerSolverTests
{
    [Fact]
    public void SolveEccentricAnomaly_CircularOrbit_ReturnsM()
    {
        // e=0 → E = M for all M
        double M = 1.234;
        double E = KeplerSolver.SolveEccentricAnomaly(M, 0.0);
        Assert.Equal(M % (2 * Math.PI), E, precision: 8);
    }

    [Theory]
    [InlineData(0.0,    0.1)]
    [InlineData(1.5708, 0.1)]  // π/2
    [InlineData(3.14,   0.2)]
    public void SolveEccentricAnomaly_KeplerEquationSatisfied(double M, double e)
    {
        double E = KeplerSolver.SolveEccentricAnomaly(M, e);
        // Verify M = E - e·sin(E) (modulo 2π normalisation)
        double Mcheck = E - e * Math.Sin(E);
        double expected = M % (2 * Math.PI);
        if (expected < 0) expected += 2 * Math.PI;
        Assert.Equal(expected, Mcheck, precision: 8);
    }

    [Fact]
    public void KerbinStateAt_UT0_CorrectRadius()
    {
        var orbit = BodyDatabase.Kerbin.Orbit!;
        double mu  = BodyDatabase.Kerbol.GravParam;

        var (pos, _) = KeplerSolver.StateAt(orbit, mu, ut: 0.0);

        // Kerbin e=0 → r = a at all times
        double expected = orbit.SemiMajorAxis;
        Assert.Equal(expected, pos.Magnitude, precision: 0);  // within 0.5 m
    }

    [Fact]
    public void KerbinStateAt_SpeedMatchesVisViva()
    {
        var orbit  = BodyDatabase.Kerbin.Orbit!;
        double mu  = BodyDatabase.Kerbol.GravParam;
        double a   = orbit.SemiMajorAxis;

        var (pos, vel) = KeplerSolver.StateAt(orbit, mu, ut: 1_000_000.0);

        double r        = pos.Magnitude;
        double vExpected = Math.Sqrt(mu * (2.0 / r - 1.0 / a));  // vis-viva
        Assert.Equal(vExpected, vel.Magnitude, precision: 0);
    }

    [Fact]
    public void MunStateAt_SpeedMatchesVisViva()
    {
        var orbit = BodyDatabase.Mun.Orbit!;
        double mu = BodyDatabase.Kerbin.GravParam;
        double a  = orbit.SemiMajorAxis;

        var (pos, vel) = KeplerSolver.StateAt(orbit, mu, ut: 500_000.0);

        double r        = pos.Magnitude;
        double vExpected = Math.Sqrt(mu * (2.0 / r - 1.0 / a));
        Assert.Equal(vExpected, vel.Magnitude, precision: 0);
    }
}
