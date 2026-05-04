namespace KspNavComputer.Api.Dtos;

public record TransferRequest(
    string Origin,
    string Destination,
    double DepartureUT,
    double TimeOfFlight,
    double OriginAltitude,
    double DestinationAltitude
);

public record TransferResponse(
    double DepartureUT,
    string DepartureDate,
    double ArrivalUT,
    string ArrivalDate,
    double EjectionDeltaV,
    double InsertionDeltaV,
    double TotalDeltaV
);
