namespace KspNavComputer.Api.Dtos;

public record TransferRequest(
    string Origin,
    string Destination,
    double DepartureUT,
    double TimeOfFlight,
    double OriginAltitude,
    double DestinationAltitude,
    double OriginInclination       = 0,         // degrees
    double DestinationInclination  = 0,          // degrees
    double OriginEccentricity      = 0,
    double DestinationEccentricity = 0,
    string TransferType            = "Optimal",  // "Ballistic" | "MidCoursePlaneChange" | "Optimal"
    bool   NoInsertionBurn         = false        // true = fly-by / aerocapture
);

public record BurnVectorDto(double Prograde, double Normal, double Radial);

public record EjectionDetailsDto(double AngleDeg, double InclinationDeg);

public record PlaneChangeBurnDto(
    double      DeltaV,
    double      BurnUT,
    string      BurnDate,
    BurnVectorDto Vector,
    string      PreciseManeuverText
);

public record BurnDto(
    double DeltaV,
    double BurnUT,
    string BurnDate,
    BurnVectorDto Vector,
    string PreciseManeuverText,
    EjectionDetailsDto? EjectionDetails
);

public record TransferResponse(
    double DepartureUT,
    string DepartureDate,
    double ArrivalUT,
    string ArrivalDate,
    BurnDto Ejection,
    BurnDto Insertion,
    double TotalDeltaV,
    PlaneChangeBurnDto? PlaneChange         = null,
    double PhaseAngleDeg                    = 0,
    double TransferAngleDeg                 = 0,
    double TransferPeriapsis                = 0,    // [m] from central body
    double TransferApoapsis                 = 0,    // [m] from central body
    double InsertionInclinationDeg          = 0
);

