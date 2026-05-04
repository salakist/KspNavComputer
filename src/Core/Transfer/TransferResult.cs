namespace KspNavComputer.Core.Transfer;

/// <summary>Result of a one-way transfer calculation.</summary>
public record TransferResult(
    double DepartureUT,         // [s UT]
    double ArrivalUT,           // [s UT]
    double EjectionDeltaV,      // Δv at departure burn [m/s]
    double InsertionDeltaV,     // Δv at arrival capture burn [m/s]
    double TotalDeltaV          // sum of the two burns [m/s]
);
