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
    double RefUT,
    /// <summary>
    /// Optional prograde reference velocity for ejection-angle calculation.
    /// When non-null, used instead of <see cref="BodyVelocity"/> as the
    /// prograde direction in <see cref="ManeuverComputer.ComputeEjectionDetails"/>.
    /// Allows the angle to be anchored to the body's velocity at departure
    /// (t0) even when <see cref="BodyVelocity"/> is at the SOI-exit time (t1).
    /// </summary>
    Vector3d? ProgradeReferenceVelocity = null
);
