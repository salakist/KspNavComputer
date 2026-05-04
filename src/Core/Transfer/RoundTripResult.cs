namespace KspNavComputer.Core.Transfer;

/// <summary>Result of a round-trip transfer calculation.</summary>
public record RoundTripResult(
    TransferResult Outbound,    // departure → destination leg
    TransferResult Return,      // destination → origin leg
    double         TotalDeltaV  // sum of all four burns [m/s]
);
