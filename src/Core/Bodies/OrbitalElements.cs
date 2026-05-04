namespace KspNavComputer.Core.Bodies;

/// <summary>
/// Keplerian orbital elements (two-body, unperturbed).
/// All angles in radians, distances in metres, time in seconds.
/// </summary>
public record OrbitalElements(
    double SemiMajorAxis,               // a [m]
    double Eccentricity,                // e  (0 = circular)
    double Inclination,                 // i [rad]
    double LongitudeOfAscendingNode,    // Ω [rad]
    double ArgumentOfPeriapsis,         // ω [rad]
    double MeanAnomalyAtEpoch,          // M₀ [rad]
    double Epoch                        // t₀ [s UT]
);
