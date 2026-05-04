namespace KspNavComputer.Api.Dtos;

public record RoundTripRequest(
    string Origin,
    string Destination,
    double DepartureUT,
    double OutboundTimeOfFlight,
    double StayDuration,
    double ReturnTimeOfFlight,
    double OriginAltitude,
    double DestinationAltitude
);

public record RoundTripResponse(
    TransferResponse Outbound,
    TransferResponse Return,
    double TotalDeltaV
);
