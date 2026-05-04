using KspNavComputer.Core.Bodies;

namespace KspNavComputer.Core.Transfer;

/// <summary>
/// Input parameters for a porkchop (Δv / departure-date × TOF) grid computation.
/// </summary>
public record PorkchopParameters(
    CelestialBody  Origin,
    CelestialBody  Destination,
    ParkingOrbit   OriginOrbit,
    ParkingOrbit?  DestinationOrbit,    // null = no insertion burn (fly-by / aerocapture)
    double         EarliestDeparture,   // [s UT]
    double         LatestDeparture,     // [s UT]
    TransferType   TransferType  = TransferType.Optimal,
    int            GridCols      = 100,
    int            GridRows      = 100
);
