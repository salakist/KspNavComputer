namespace KspNavComputer.Core.Transfer;

/// <summary>
/// Selects the transfer computation strategy.
/// </summary>
public enum TransferType
{
    /// <summary>Pure Lambert transfer; no mid-course manoeuvre.</summary>
    Ballistic,

    /// <summary>
    /// Mid-course plane-change burn at the optimised true anomaly.
    /// Always uses a plane-change burn, even if the inclination difference is tiny.
    /// </summary>
    MidCoursePlaneChange,

    /// <summary>
    /// Run both Ballistic and MidCoursePlaneChange and return the lower total Δv.
    /// Skips plane-change computation when the transfer angle is ≤ 90° or the
    /// destination is nearly coplanar with the origin.
    /// </summary>
    Optimal,
}
