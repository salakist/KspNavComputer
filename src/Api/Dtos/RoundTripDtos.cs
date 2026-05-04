namespace KspNavComputer.Api.Dtos;

public record RoundTripRequest(
    string Origin,
    string Destination,
    double DepartureUT,
    double OutboundTimeOfFlight,
    double StayDuration,
    double ReturnTimeOfFlight,
    double OriginAltitude,
    double DestinationAltitude,
    double OriginInclination       = 0,  // degrees
    double DestinationInclination  = 0,  // degrees
    double OriginEccentricity      = 0,
    double DestinationEccentricity = 0
);

public record RoundTripResponse(
    TransferResponse Outbound,
    TransferResponse Return,
    double TotalDeltaV
);
