namespace KspNavComputer.Core.Transfer;

/// <summary>Result of a one-way transfer calculation.</summary>
public record TransferResult(
    double          DepartureUT,          // [s UT] — heliocentric departure time
    double          ArrivalUT,            // [s UT] — heliocentric arrival time
    Burn            Ejection,             // ejection burn details (Δv, UT, vector)
    Burn            Insertion,            // insertion burn details (Δv, UT, vector)
    double          TotalDeltaV,          // sum of all burns [m/s]
    Burn?            PlaneChange          = null, // mid-course plane change (null if ballistic)
    double          PhaseAngleDeg        = 0,    // angle between origin and destination at departure [°]
    double          TransferAngleDeg     = 0,    // angle swept during transfer (0–360°) [°]
    double          TransferPeriapsis    = 0,    // transfer orbit periapsis distance from central body [m]
    double          TransferApoapsis     = 0,    // transfer orbit apoapsis distance from central body [m]
    double          InsertionInclinationDeg = 0  // inclination of insertion hyperbola relative to ecliptic [°]
);
