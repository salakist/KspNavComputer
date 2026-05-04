namespace KspNavComputer.Core.Transfer;

/// <summary>Result of a one-way transfer calculation.</summary>
public record TransferResult(
    double DepartureUT,              // [s UT] — heliocentric departure time
    double ArrivalUT,                // [s UT] — heliocentric arrival time
    double EjectionDeltaV,           // Δv magnitude of the ejection burn [m/s]
    double InsertionDeltaV,          // Δv magnitude of the insertion burn [m/s]
    double TotalDeltaV,              // sum of both burns [m/s]
    double EjectionBurnUT,           // [s UT] — precise periapsis ejection burn time
    double InsertionBurnUT,          // [s UT] — precise periapsis insertion burn time
    BurnVector EjectionBurnVector,   // prograde/normal/radial at ejection periapsis [m/s]
    BurnVector InsertionBurnVector   // prograde/normal/radial at insertion periapsis [m/s]
);
