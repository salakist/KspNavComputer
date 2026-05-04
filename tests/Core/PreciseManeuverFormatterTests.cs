using KspNavComputer.Core.Time;
using KspNavComputer.Core.Transfer;
using Xunit;

namespace KspNavComputer.Core.Tests;

/// <summary>
/// Tests for PreciseManeuverFormatter.
/// Reference: KspTime.FromKspCalendar(3, 266, 0, 47, 43) = 24 133 663 s
/// which the PM format displays as "2y, 266d, 0h, 47m, 43s" (0-indexed year).
/// </summary>
public class PreciseManeuverFormatterTests
{
    // UT = (3-1)*9203400 + (266-1)*21600 + 47*60 + 43 = 24133663
    private static readonly double ReferenceUT = KspTime.FromKspCalendar(3, 266, 0, 47, 43);

    [Fact]
    public void Format_FullBlock_MatchesExpectedLayout()
    {
        var burn = new Burn(741.1, ReferenceUT, new BurnVector(741.1, 0.0, 0.0));

        var expected = string.Join("\n",
            "Precise Maneuver Information",
            "Depart at:      2y, 266d, 0h, 47m, 43s",
            "       UT:      24133663",
            "Prograde \u0394v:    741.1 m/s",
            "Normal \u0394v:      0.0 m/s",
            "Radial \u0394v:      0.0 m/s",
            "Total \u0394v:       741 m/s"
        );

        Assert.Equal(expected, PreciseManeuverFormatter.Format(burn));
    }

    [Fact]
    public void Format_WithEjectionDetails_IncludesAngleAndInclination()
    {
        // Example from increment-2.md: 113.73° to retrograde, inc 0.02°
        var ejection = new EjectionDetails(-113.73, 0.02);
        var burn = new Burn(1161.4, ReferenceUT, new BurnVector(1161.4, 0.0, 0.0), ejection);

        var text = PreciseManeuverFormatter.Format(burn);

        Assert.Contains("Ejection Angle: 113.73° to retrograde", text);
        Assert.Contains("Ejection Inc.:  0.02°", text);
    }

    [Fact]
    public void Format_WithEjectionPrograde_ShowsToPrograde()
    {
        var ejection = new EjectionDetails(45.0, 1.5);
        var burn = new Burn(1000.0, ReferenceUT, new BurnVector(1000.0, 0.0, 0.0), ejection);

        Assert.Contains("Ejection Angle: 45.00° to prograde", PreciseManeuverFormatter.Format(burn));
    }

    [Fact]
    public void Format_EjectionLinesAreBetweenUtAndDeltaVLines()
    {
        var ejection = new EjectionDetails(-113.73, 0.02);
        var burn = new Burn(1161.4, ReferenceUT, new BurnVector(1161.4, 0.0, 0.0), ejection);

        var lines = PreciseManeuverFormatter.Format(burn).Split('\n');

        // Expected order: header, depart, UT, ejection angle, ejection inc, prograde, normal, radial, total
        Assert.Equal(9, lines.Length);
        Assert.StartsWith("Ejection Angle:", lines[3]);
        Assert.StartsWith("Ejection Inc.:", lines[4]);
        Assert.StartsWith("Prograde", lines[5]);
    }

    [Fact]
    public void Format_Year_IsZeroIndexed()
    {
        // KSP Year 3 (1-indexed) should display as "2y"
        var burn = new Burn(100.0, ReferenceUT, BurnVector.Zero);
        Assert.Contains("2y, 266d,", PreciseManeuverFormatter.Format(burn));
    }

    [Fact]
    public void Format_TotalDeltaV_IsRoundedInteger()
    {
        // 741.1 m/s rounds to 741
        var burn = new Burn(741.1, ReferenceUT, BurnVector.Zero);
        Assert.Contains("Total \u0394v:       741 m/s", PreciseManeuverFormatter.Format(burn));
    }

    [Fact]
    public void Format_UtZero_ShowsYear0Day1()
    {
        // UT 0 = KSP Year 1 Day 1 → PM: 0y, 1d, 0h, 0m, 0s
        var burn = new Burn(100.0, 0.0, BurnVector.Zero);
        Assert.Contains("0y, 1d, 0h, 0m, 0s", PreciseManeuverFormatter.Format(burn));
    }

    [Fact]
    public void Format_ExactYearBoundary_DayResetsToOne()
    {
        // UT = 2 * SecondsPerYear → 2y, 1d, 0h, 0m, 0s
        var burn = new Burn(100.0, 2 * KspTime.SecondsPerYear, BurnVector.Zero);
        Assert.Contains("2y, 1d, 0h, 0m, 0s", PreciseManeuverFormatter.Format(burn));
    }
}
