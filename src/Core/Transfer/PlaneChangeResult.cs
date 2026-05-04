using KspNavComputer.Core.Mechanics;

namespace KspNavComputer.Core.Transfer;

/// <summary>
/// Result of a mid-course plane-change transfer computation.
/// Contains the heliocentric departure and arrival velocities together with
/// the plane-change burn that occurs between them.
/// </summary>
internal sealed record PlaneChangeResult(
    Vector3d DepartureVelocity,   // heliocentric velocity at departure [m/s]
    Vector3d ArrivalVelocity,     // heliocentric velocity at arrival after rotation [m/s]
    Burn     PlaneChange          // mid-course plane-change manoeuvre
);
