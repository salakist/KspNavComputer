namespace KspNavComputer.Core.Maneuver;

/// <summary>
/// Ejection geometry for a departure burn: the angle from the origin body's
/// prograde direction to the periapsis of the escape hyperbola, and the
/// inclination of that hyperbola relative to the body's orbital plane.
///
/// Only meaningful for ejection burns where the spacecraft actually escapes a
/// body's SOI; null for insertion burns.
///
/// Angle convention: positive = ahead of prograde, negative = behind (retrograde side).
/// This matches the Precise Maneuver mod's copy-to-clipboard format.
/// </summary>
public record EjectionDetails(
    double AngleDeg,        // signed: + = to prograde, − = to retrograde  [°]
    double InclinationDeg   // inclination of escape hyperbola [°]
);
