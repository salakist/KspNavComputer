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
    /// Year is 0-indexed; day is 1-indexed (matching the mod's display convention).
    /// All decimal separators use invariant culture (period).
    /// Ejection angle and inclination lines are included when
    /// <see cref="Burn.Ejection"/> is non-null and finite.
    /// </summary>
    public static string Format(Burn burn)
    {
        var c    = KspTime.ToKspCalendar(burn.BurnUT);
        var date = $"{c.Year - 1}y, {c.Day}d, {c.Hour}h, {c.Minute}m, {c.Second}s";
        var ut   = (long)Math.Round(burn.BurnUT);

        var ic = CultureInfo.InvariantCulture;

        var lines = new System.Collections.Generic.List<string>
        {
            "Precise Maneuver Information",
            $"{"Depart at:".PadRight(16)}{date}",
            $"{"       UT:".PadRight(16)}{ut}",
        };

        if (burn.Ejection is { } ej
            && !double.IsNaN(ej.AngleDeg)
            && !double.IsNaN(ej.InclinationDeg))
        {
            // Positive = "to prograde", negative = "to retrograde" (PM mod convention)
            string angleStr = ej.AngleDeg >= 0
                ? $"{ej.AngleDeg.ToString("F2", ic)}° to prograde"
                : $"{(-ej.AngleDeg).ToString("F2", ic)}° to retrograde";
            lines.Add($"{"Ejection Angle:".PadRight(16)}{angleStr}");
            lines.Add($"{"Ejection Inc.:".PadRight(16)}{ej.InclinationDeg.ToString("F2", ic)}°");
        }

        lines.Add($"{"Prograde \u0394v:".PadRight(16)}{burn.Vector.Prograde.ToString("F1", ic)} m/s");
        lines.Add($"{"Normal \u0394v:".PadRight(16)}{burn.Vector.Normal.ToString("F1", ic)} m/s");
        lines.Add($"{"Radial \u0394v:".PadRight(16)}{burn.Vector.Radial.ToString("F1", ic)} m/s");
        lines.Add($"{"Total \u0394v:".PadRight(16)}{(long)Math.Round(burn.DeltaV)} m/s");

        return string.Join("\n", lines);
    }
}
