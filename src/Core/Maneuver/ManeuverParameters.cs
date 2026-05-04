using KspNavComputer.Core.Bodies;
using KspNavComputer.Core.Mechanics;

namespace KspNavComputer.Core.Maneuver;

/// <summary>
/// Input parameters for <see cref="ManeuverComputer"/>.
/// </summary>
internal record ManeuverParameters(
    ParkingOrbit ParkingOrbit,
    CelestialBody Body,
    Vector3d TransferVelocity,
    Vector3d BodyVelocity,
    bool IsEjection,
    double RefUT
);
