namespace KspNavComputer.Core.Porkchop;

/// <summary>
/// Output of <see cref="PorkchopComputer.AutoTofRange"/>: the computed min/max
/// time-of-flight bounds for a porkchop grid.
/// </summary>
public record AutoTofRangeResult(
    double MinTof, // [s]
    double MaxTof  // [s]
);
