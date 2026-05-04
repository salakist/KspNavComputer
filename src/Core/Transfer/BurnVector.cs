namespace KspNavComputer.Core.Transfer;

/// <summary>
/// Δv components of a maneuver in the local orbital frame at the burn point.
///
/// <list type="bullet">
///   <item><b>Prograde</b> — tangential, in the direction of orbital motion.
///     Positive = prograde, negative = retrograde.</item>
///   <item><b>Normal</b>   — perpendicular to the orbital plane, toward north.
///     Positive = toward orbital north (right-hand rule relative to prograde).</item>
///   <item><b>Radial</b>   — along the radius vector, away from the central body.
///     Currently always 0; reserved for future burns requiring a radial component.</item>
/// </list>
///
/// All values in m/s.
/// </summary>
public record BurnVector(double Prograde, double Normal, double Radial)
{
    /// <summary>Zero burn vector — no manoeuvre.</summary>
    public static readonly BurnVector Zero = new(0.0, 0.0, 0.0);

    /// <summary>Magnitude of the burn [m/s].</summary>
    public double Magnitude => Math.Sqrt(Prograde * Prograde + Normal * Normal + Radial * Radial);
}
