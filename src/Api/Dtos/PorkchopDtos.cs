namespace KspNavComputer.Api.Dtos;

public record PorkchopRequest(
    string Origin,
    string Destination,
    double OriginAltitude,
    double DestinationAltitude,
    double EarliestDeparture,       // [s UT]
    double LatestDeparture,         // [s UT]
    double OriginInclination       = 0,         // degrees
    double DestinationInclination  = 0,          // degrees
    double OriginEccentricity      = 0,
    double DestinationEccentricity = 0,
    bool   NoInsertionBurn         = false,
    string TransferType            = "Optimal",
    int    GridCols                = 100,
    int    GridRows                = 100
);

public record PorkchopResponse(
    double[] DeltaVs,           // flat row-major [Rows × Cols], NaN for failed cells
    int      Rows,
    int      Cols,
    double   EarliestDeparture, // [s UT]
    double   LatestDeparture,   // [s UT]
    double   MinTof,            // auto-computed [s]
    double   MaxTof,            // auto-computed [s]
    double   MinDeltaV,         // [m/s]
    double   MaxDeltaV,         // [m/s]
    double   MeanLogDeltaV,
    double   StdLogDeltaV,
    int      OptimalRow,
    int      OptimalCol
);
