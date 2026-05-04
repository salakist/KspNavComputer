namespace KspNavComputer.Core.Transfer;

/// <summary>
/// A single manoeuvre: its Δv magnitude, the precise periapsis burn UT,
/// and the burn vector components (prograde/normal/radial) in the local
/// orbital frame at the burn point.
/// </summary>
public record Burn(
    double DeltaV,       // total Δv magnitude [m/s]
    double BurnUT,       // precise periapsis burn time [s UT]
    BurnVector Vector    // prograde/normal/radial components [m/s]
);
