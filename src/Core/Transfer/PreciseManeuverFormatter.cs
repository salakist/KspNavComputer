using System.Collections.Generic;
using System.Globalization;
using KspNavComputer.Core.Time;

namespace KspNavComputer.Core.Transfer;

/// <summary>
/// Formats a <see cref="Burn"/> as the Precise Maneuver mod plaintext block.
/// Intended for both the web API and the future KSP mod DLL.
/// </summary>
public static class PreciseManeuverFormatter
{
    /// <summary>
    /// Returns the Precise Maneuver plaintext block for the given burn.
    /// Year and day are 0-indexed (matching KSP's display convention and the PM mod).
    /// All decimal separators use invariant culture (period).
    /// Ejection angle and inclination are NOT included — the PM mod treats that
    /// line as a node repositioning command on paste, which would shift the UT.
    /// </summary>
    public static string Format(Burn burn)
    {
        var c    = KspTime.ToKspCalendar(burn.BurnUT);
        var date = $"{c.Year - 1}y, {c.Day - 1}d, {c.Hour}h, {c.Minute}m, {c.Second}s";
        var ut   = (long)Math.Round(burn.BurnUT);

        var ic = CultureInfo.InvariantCulture;

        var lines = new List<string>
        {
            "Precise Maneuver Information",
            "Depart at:".PadRight(16) + date,
            "       UT:".PadRight(16) + ut,
        };

        lines.Add("Prograde \u0394v:".PadRight(16) + burn.Vector.Prograde.ToString("F1", ic) + " m/s");
        lines.Add("Normal \u0394v:".PadRight(16) + burn.Vector.Normal.ToString("F1", ic) + " m/s");
        lines.Add("Radial \u0394v:".PadRight(16) + burn.Vector.Radial.ToString("F1", ic) + " m/s");
        lines.Add("Total \u0394v:".PadRight(16) + (long)Math.Round(burn.DeltaV) + " m/s");

        return string.Join("\n", lines);
    }
}
