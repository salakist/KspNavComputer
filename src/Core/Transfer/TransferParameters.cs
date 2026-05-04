using KspNavComputer.Core.Bodies;
using KspNavComputer.Core.Maneuver;

namespace KspNavComputer.Core.Transfer;

/// <summary>Input parameters for a one-way transfer calculation.</summary>
public record TransferParameters(
    CelestialBody Origin,
    CelestialBody Destination,
    double        DepartureUT,                               // [s UT]
    double        TimeOfFlight,                              // [s]
    ParkingOrbit  OriginOrbit,
    ParkingOrbit  DestinationOrbit,
    TransferType  TransferType    = TransferType.Optimal,
    bool          NoInsertionBurn = false                    // true = fly-by / aerocapture
);
