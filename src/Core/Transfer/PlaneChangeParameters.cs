using KspNavComputer.Core.Mechanics;

namespace KspNavComputer.Core.Transfer;

/// <summary>
/// Input parameters for <see cref="PlaneChangeComputer"/>.
/// </summary>
internal record PlaneChangeParameters(
    Vector3d R1,
    Vector3d V1Body,
    Vector3d R2,
    double TimeOfFlight,
    double DepartureUT,
    double Mu
);
