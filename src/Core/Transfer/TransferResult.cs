namespace KspNavComputer.Core.Transfer;

/// <summary>Result of a one-way transfer calculation.</summary>
public record TransferResult(
    double DepartureUT,   // [s UT] — heliocentric departure time
    double ArrivalUT,     // [s UT] — heliocentric arrival time
    Burn   Ejection,      // ejection burn details (Δv, UT, vector)
    Burn   Insertion,     // insertion burn details (Δv, UT, vector)
    double TotalDeltaV    // sum of both burns [m/s]
);
