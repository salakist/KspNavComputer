namespace KspNavComputer.Core.Transfer;

/// <summary>
/// A mid-course plane-change manoeuvre on the heliocentric transfer arc.
///
/// The burn occurs at a true anomaly offset from the arrival point, chosen
/// to minimise total mission Δv (ported from LWP optimalPlaneChange logic).
///
/// Burn vector components are in the spacecraft's local orbital frame at
/// the burn point:
///   Prograde = −|ΔV| · |sin(Δi/2)|   (slightly retrograde)
///   Normal   =  |ΔV| · sign(Δi) · cos(Δi/2)   (out-of-plane)
///   Radial   = 0
/// where Δi = plane-change angle (inclination delta [rad]).
/// </summary>
public record PlaneChangeBurn(
    double    DeltaV,   // total Δv magnitude [m/s]
    double    BurnUT,   // burn time [s UT]
    BurnVector Vector   // prograde/normal/radial components [m/s]
);
