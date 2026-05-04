using KspNavComputer.Core.Time;
using KspNavComputer.Core.Transfer;
using Xunit;

namespace KspNavComputer.Core.Tests;

/// <summary>
/// Tests for PreciseManeuverFormatter.
/// Reference: KspTime.FromKspCalendar(3, 266, 0, 47, 43) = 24 133 663 s
/// which the PM format displays as "2y, 265d, 0h, 47m, 43s" (both year and day 0-indexed).
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
            "Depart at:      2y, 265d, 0h, 47m, 43s",
            "       UT:      24133663",
            "Prograde \u0394v:    741.1 m/s",
            "Normal \u0394v:      0.0 m/s",
            "Radial \u0394v:      0.0 m/s",
            "Total \u0394v:       741 m/s"
        );

        Assert.Equal(expected, PreciseManeuverFormatter.Format(burn));
    }

    [Fact]
    public void Format_WithEjectionDetails_DoesNotIncludeAngleInText()
    {
        // Ejection Angle/Inc are display-only. Including them in the clipboard
        // would cause PM to reposition the node on paste (PM treats that line as
        // a node repositioning command, not an annotation).
        var ejection = new EjectionDetails(-113.73, 0.02);
        var burn = new Burn(1161.4, ReferenceUT, new BurnVector(1161.4, 0.0, 0.0), ejection);

        var text = PreciseManeuverFormatter.Format(burn);

        Assert.DoesNotContain("Ejection Angle:", text);
        Assert.DoesNotContain("Ejection Inc.:", text);
    }

    [Fact]
    public void Format_WithEjectionDetails_HasSevenLines()
    {
        // No ejection lines in clipboard text: 7 lines total regardless of Ejection presence.
        var ejection = new EjectionDetails(45.0, 1.5);
        var burn = new Burn(1000.0, ReferenceUT, new BurnVector(1000.0, 0.0, 0.0), ejection);

        var lines = PreciseManeuverFormatter.Format(burn).Split('\n');

        Assert.Equal(7, lines.Length);
        Assert.StartsWith("Prograde", lines[3]);
    }

    [Fact]
    public void Format_WithoutEjectionDetails_HasSevenLines()
    {
        var burn = new Burn(1000.0, ReferenceUT, new BurnVector(1000.0, 0.0, 0.0));

        var lines = PreciseManeuverFormatter.Format(burn).Split('\n');

        Assert.Equal(7, lines.Length);
        Assert.StartsWith("Prograde", lines[3]);
    }

    [Fact]
    public void Format_Year_IsZeroIndexed()
    {
        // KSP Year 3 (1-indexed) should display as "2y"
        var burn = new Burn(100.0, ReferenceUT, BurnVector.Zero);
        Assert.Contains("2y, 265d,", PreciseManeuverFormatter.Format(burn));
    }

    [Fact]
    public void Format_TotalDeltaV_IsRoundedInteger()
    {
        // 741.1 m/s rounds to 741
        var burn = new Burn(741.1, ReferenceUT, BurnVector.Zero);
        Assert.Contains("Total \u0394v:       741 m/s", PreciseManeuverFormatter.Format(burn));
    }

    [Fact]
    public void Format_UtZero_ShowsYear0Day0()
    {
        // UT 0 = KSP Year 1 Day 1 → PM: 0y, 0d, 0h, 0m, 0s
        var burn = new Burn(100.0, 0.0, BurnVector.Zero);
        Assert.Contains("0y, 0d, 0h, 0m, 0s", PreciseManeuverFormatter.Format(burn));
    }

    [Fact]
    public void Format_ExactYearBoundary_DayResetsToZero()
    {
        // UT = 2 * SecondsPerYear → 2y, 0d, 0h, 0m, 0s
        var burn = new Burn(100.0, 2 * KspTime.SecondsPerYear, BurnVector.Zero);
        Assert.Contains("2y, 0d, 0h, 0m, 0s", PreciseManeuverFormatter.Format(burn));
    }
}
