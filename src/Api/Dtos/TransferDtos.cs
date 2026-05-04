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

public record BurnVectorDto(double Prograde, double Normal, double Radial);

public record BurnDto(
    double DeltaV,
    double BurnUT,
    string BurnDate,
    BurnVectorDto Vector
);

public record TransferResponse(
    double DepartureUT,
    string DepartureDate,
    double ArrivalUT,
    string ArrivalDate,
    BurnDto Ejection,
    BurnDto Insertion,
    double TotalDeltaV
);
