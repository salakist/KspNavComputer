using KspNavComputer.Core.Bodies;
using KspNavComputer.Core.Transfer;
using Xunit;

namespace KspNavComputer.Core.Tests;

/// <summary>
/// Tests for TransferComputer.ComputeRoundTrip (Increment 1b).
///
/// Uses the well-characterised Kerbin → Duna window from the reference suite
/// as the outbound leg, then a follow-on Duna → Kerbin return leg.
/// We verify structural correctness (timing chain, positive Δv) and that the
/// total mission Δv is in a physically plausible range for a Duna round trip.
/// </summary>
public class RoundTripTransferTests
{
    private static readonly ParkingOrbit LkoPark = new(Altitude: 100_000);   // 100 km LKO
    private static readonly ParkingOrbit LdoPark = new(Altitude:  60_000);   // 60 km LDO

    // Outbound window from the reference suite (within ±1 % of LWP oracle)
    private const double DepartureUT          = 4_536_870.0;
    private const double OutboundTof          = 4_557_600.0;   // 211 Kerbin days
    private const double StayDuration         = 7_776_000.0;   // ~90 Kerbin days
    private const double ReturnTof            = 5_400_000.0;   // 250 Kerbin days

    private static RoundTripParameters MakeParams() => new(
        Origin:               BodyDatabase.Kerbin,
        Destination:          BodyDatabase.Duna,
        DepartureUT:          DepartureUT,
        OutboundTimeOfFlight: OutboundTof,
        StayDuration:         StayDuration,
        ReturnTimeOfFlight:   ReturnTof,
        OriginOrbit:          LkoPark,
        DestinationOrbit:     LdoPark
    );

    [Fact]
    public void RoundTrip_TimingChain_IsConsistent()
    {
        var result = TransferComputer.ComputeRoundTrip(MakeParams());

        // Outbound leg
        Assert.Equal(DepartureUT,              result.Outbound.DepartureUT);
        Assert.Equal(DepartureUT + OutboundTof, result.Outbound.ArrivalUT);

        // Return leg departs after stay
        double expectedReturnDep = result.Outbound.ArrivalUT + StayDuration;
        Assert.Equal(expectedReturnDep,              result.Return.DepartureUT);
        Assert.Equal(expectedReturnDep + ReturnTof,  result.Return.ArrivalUT);
    }

    [Fact]
    public void RoundTrip_AllDeltaVValues_ArePositive()
    {
        var result = TransferComputer.ComputeRoundTrip(MakeParams());

        Assert.True(result.Outbound.Ejection.DeltaV  > 0, "Outbound ejection Δv must be positive.");
        Assert.True(result.Outbound.Insertion.DeltaV > 0, "Outbound insertion Δv must be positive.");
        Assert.True(result.Return.Ejection.DeltaV    > 0, "Return ejection Δv must be positive.");
        Assert.True(result.Return.Insertion.DeltaV   > 0, "Return insertion Δv must be positive.");
    }

    [Fact]
    public void RoundTrip_TotalDeltaV_EqualsSum()
    {
        var result = TransferComputer.ComputeRoundTrip(MakeParams());

        double expected = result.Outbound.TotalDeltaV + result.Return.TotalDeltaV;
        Assert.Equal(expected, result.TotalDeltaV, precision: 6);
    }

    [Fact]
    public void RoundTrip_TotalDeltaV_InPlausibleRange()
    {
        // Kerbin→Duna→Kerbin round trip with a fixed (non-optimal) return window.
        // Non-optimal windows can be costly; the check just verifies we get a
        // finite, physically plausible result (not NaN/Infinity).
        var result = TransferComputer.ComputeRoundTrip(MakeParams());

        Assert.InRange(result.TotalDeltaV, 2_000, 50_000);
    }

    [Fact]
    public void RoundTrip_GreaterThanOutbound()
    {
        var result = TransferComputer.ComputeRoundTrip(MakeParams());

        Assert.True(result.TotalDeltaV > result.Outbound.TotalDeltaV,
            "Round-trip total Δv must exceed the outbound leg alone.");
    }

    [Fact]
    public void RoundTrip_SameBodyThrows()
    {
        var p = new RoundTripParameters(
            Origin:               BodyDatabase.Kerbin,
            Destination:          BodyDatabase.Kerbin,
            DepartureUT:          DepartureUT,
            OutboundTimeOfFlight: OutboundTof,
            StayDuration:         StayDuration,
            ReturnTimeOfFlight:   ReturnTof,
            OriginOrbit:          LkoPark,
            DestinationOrbit:     LkoPark
        );

        Assert.Throws<ArgumentException>(() => TransferComputer.ComputeRoundTrip(p));
    }
}
