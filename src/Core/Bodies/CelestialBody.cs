namespace KspNavComputer.Core.Bodies;

/// <summary>
/// Represents a celestial body in the Kerbol system.
/// All values are in SI units (metres, seconds, m³/s²).
/// </summary>
public record CelestialBody(
    string Name,
    double GravParam,               // μ = GM  [m³/s²]
    double Radius,                  // equatorial radius [m]
    double SphereOfInfluence,       // SOI radius [m]
    double SiderealRotationPeriod,  // [s]
    CelestialBody? Parent,          // null for the central star
    OrbitalElements? Orbit          // null for the central star
);
