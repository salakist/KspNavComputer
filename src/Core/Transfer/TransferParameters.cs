using KspNavComputer.Core.Bodies;

namespace KspNavComputer.Core.Transfer;

/// <summary>Input parameters for a one-way transfer calculation.</summary>
public record TransferParameters(
    CelestialBody Origin,
    CelestialBody Destination,
    double        DepartureUT,      // [s UT]
    double        TimeOfFlight,     // [s]
    ParkingOrbit  OriginOrbit,
    ParkingOrbit  DestinationOrbit
);
