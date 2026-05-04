using KspNavComputer.Core.Bodies;

namespace KspNavComputer.Core.Transfer;

/// <summary>Input parameters for a round-trip (outbound + return) transfer.</summary>
public record RoundTripParameters(
    CelestialBody Origin,
    CelestialBody Destination,
    double        DepartureUT,              // [s UT] outbound departure
    double        OutboundTimeOfFlight,     // [s]
    double        StayDuration,             // [s] time spent at destination
    double        ReturnTimeOfFlight,       // [s]
    ParkingOrbit  OriginOrbit,
    ParkingOrbit  DestinationOrbit
);
