namespace KspNavComputer.Core.Transfer;

/// <summary>
/// Output of a porkchop grid computation.
///
/// The grid is row-major: index = row * Cols + col.
///   col → departure date (0 = EarliestDeparture, Cols-1 = LatestDeparture)
///   row → time of flight (0 = MinTof, Rows-1 = MaxTof)
///
/// Cells that fail to converge (degenerate Lambert geometry etc.) contain
/// <see cref="double.NaN"/>.
///
/// Log-scale statistics (<see cref="MeanLogDeltaV"/>, <see cref="StdLogDeltaV"/>)
/// are computed from valid (non-NaN, positive) cells and are used by the UI to
/// normalise the colour map to ±2σ around the log-mean, clipping extreme values.
/// </summary>
public record PorkchopResult(
    double[] DeltaVs,           // flat row-major array [Rows × Cols]
    int      Rows,
    int      Cols,
    double   EarliestDeparture, // [s UT]
    double   LatestDeparture,   // [s UT]
    double   MinTof,            // [s]
    double   MaxTof,            // [s]
    double   MinDeltaV,         // global minimum valid Δv [m/s]
    double   MaxDeltaV,         // global maximum valid Δv [m/s]
    double   MeanLogDeltaV,     // mean of log(Δv) for valid cells
    double   StdLogDeltaV,      // std-dev of log(Δv) for valid cells
    int      OptimalRow,        // row index of minimum-Δv cell
    int      OptimalCol         // col index of minimum-Δv cell
);
