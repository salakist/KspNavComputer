namespace KspNavComputer.Core.Transfer;

/// <summary>
/// Describes the circular, equatorial parking orbit at departure or arrival.
/// Inclination and eccentricity are fixed at 0 for Increment 1a.
/// </summary>
public record ParkingOrbit(
    double Altitude,        // altitude above body surface [m]
    double Inclination = 0, // [rad] — reserved for 1c
    double Eccentricity = 0 // — reserved for 1c
);
