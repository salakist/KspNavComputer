namespace KspNavComputer.Core.Transfer;

/// <summary>
/// Describes the parking orbit at departure or arrival.
/// </summary>
/// <param name="Altitude">Periapsis altitude above body surface [m].</param>
/// <param name="Inclination">
///   Orbital inclination relative to the body's equatorial plane [rad].
///   An inclined parking orbit reduces ejection Δv when the departure
///   hyperbola's asymptote lies within the orbit's reachable declination band.
/// </param>
/// <param name="Eccentricity">
///   Orbital eccentricity (0 = circular).  The ejection/insertion burn is
///   performed at periapsis (<see cref="Altitude"/>).
/// </param>
public record ParkingOrbit(
    double Altitude,
    double Inclination  = 0,
    double Eccentricity = 0
);
