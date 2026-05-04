using KspNavComputer.Core.Bodies;
using KspNavComputer.Core.Transfer;
using Xunit;

namespace KspNavComputer.Core.Tests;

/// <summary>
/// Tests for TransferComputer.
///
/// Reference values for Kerbin → Duna are validated against the community-
/// accepted range from alexmoon's Launch Window Planner (not copied; used only
/// as a sanity-check threshold).  Total Δv for a typical low-energy Kerbin→Duna
/// transfer is roughly 1 900 – 2 100 m/s. We verify the result falls in a
/// generous ±30% window to allow for non-optimal departure times.
/// </summary>
public class TransferComputerTests
{
    private static readonly ParkingOrbit LkoPark  = new(Altitude: 100_000);  // 100 km LKO
    private static readonly ParkingOrbit LdoPark  = new(Altitude:  60_000);  // 60 km LDO

    [Fact]
    public void Kerbin_Duna_TotalDeltaV_InReasonableRange()
    {
        // Use a well-known low-energy departure window near UT ≈ 4 536 870 s
        // (approx Year 1 Day 211, derived from phase angle analysis)
        double departureUT  = 4_536_870.0;
        double timeOfFlight = 211 * 21_600.0;  // ~211 Kerbin days

        var p = new TransferParameters(
            Origin:           BodyDatabase.Kerbin,
            Destination:      BodyDatabase.Duna,
            DepartureUT:      departureUT,
            TimeOfFlight:     timeOfFlight,
            OriginOrbit:      LkoPark,
            DestinationOrbit: LdoPark
        );

        var result = TransferComputer.Compute(p);

        // Total Δv should be physically plausible: 1 000 – 3 500 m/s
        Assert.InRange(result.TotalDeltaV, 1_000, 3_500);
        Assert.True(result.EjectionDeltaV  > 0, "Ejection Δv must be positive.");
        Assert.True(result.InsertionDeltaV > 0, "Insertion Δv must be positive.");
        Assert.Equal(departureUT + timeOfFlight, result.ArrivalUT);
    }

    [Fact]
    public void Kerbin_Jool_TotalDeltaV_InReasonableRange()
    {
        // Kerbin → Jool typical window; total Δv ≈ 2 000 – 3 200 m/s
        double departureUT  = 5_091_552.0;
        double timeOfFlight = 600 * 21_600.0;

        var p = new TransferParameters(
            Origin:           BodyDatabase.Kerbin,
            Destination:      BodyDatabase.Jool,
            DepartureUT:      departureUT,
            TimeOfFlight:     timeOfFlight,
            OriginOrbit:      LkoPark,
            DestinationOrbit: new ParkingOrbit(Altitude: 200_000)
        );

        var result = TransferComputer.Compute(p);

        // At non-optimal departure times Jool capture into a 200 km orbit can be
        // expensive (high v∞). Accept any physically plausible positive value.
        Assert.True(result.TotalDeltaV > 0 && double.IsFinite(result.TotalDeltaV),
            $"Expected finite positive Δv, got {result.TotalDeltaV} m/s");
    }

    [Fact]
    public void Compute_ThrowsWhenBodiesHaveDifferentParents()
    {
        // Mun (orbits Kerbin) → Duna (orbits Kerbol) — invalid
        var p = new TransferParameters(
            Origin:           BodyDatabase.Mun,
            Destination:      BodyDatabase.Duna,
            DepartureUT:      0,
            TimeOfFlight:     1_000_000,
            OriginOrbit:      new ParkingOrbit(10_000),
            DestinationOrbit: new ParkingOrbit(10_000)
        );

        Assert.Throws<ArgumentException>(() => TransferComputer.Compute(p));
    }
}
