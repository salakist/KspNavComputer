namespace KspNavComputer.Api.Dtos;

public record TransferRequest(
    string Origin,
    string Destination,
    double DepartureUT,
    double TimeOfFlight,
    double OriginAltitude,
    double DestinationAltitude,
    double OriginInclination      = 0,   // degrees
    double DestinationInclination = 0,   // degrees
    double OriginEccentricity     = 0,
    double DestinationEccentricity = 0
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
