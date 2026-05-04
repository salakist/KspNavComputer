using KspNavComputer.Core.Bodies;
using KspNavComputer.Core.Transfer;

namespace KspNavComputer.Core.Tests;

/// <summary>
/// Verifies the Increment 1c extension: inclined and elliptical parking orbits
/// correctly modify ejection Δv in <see cref="TransferComputer.Compute"/>.
///
/// Strategy
/// --------
/// • Using a 90° (polar) parking orbit forces deltaI = max(0, |α|−90°) = 0,
///   which collapses the law-of-cosines to |v_hyper − v_park|.  This isolates
///   the eccentricity effect from the inclination effect.
/// • Inclination effect is tested by comparing equatorial (0°) against polar
///   (90°): the polar orbit can only equal or improve ejection Δv.
/// • Eccentricity effect is tested by the analytical formula:
///     Δv_reduction = v_park_peri(e) − v_circ = √(μ·(1+e)/r) − √(μ/r)
///   which is exact when deltaI = 0.
/// </summary>
public class InclinedEllipticalOrbitTests
{
    // Reference transfer: Kerbin → Duna, year 2, 211 KSP days TOF.
    private static readonly CelestialBody Kerbin = BodyDatabase.Kerbin;
    private static readonly CelestialBody Duna   = BodyDatabase.Duna;
    private const double DepartureUT  = 1 * 9_203_400.0;   // start of KSP year 2
    private const double Tof          = 211 * 21_600.0;     // 211 KSP days
    private const double OriginAlt    = 100_000;            // 100 km
    private const double DestAlt      = 60_000;             // 60 km

    // ---------------------------------------------------------------------------
    // Inclination tests
    // ---------------------------------------------------------------------------

    [Fact(DisplayName = "Polar (90°) origin orbit: ejection Δv ≤ equatorial")]
    public void PolarParkingOrbit_EjectionDeltaV_DoesNotExceedEquatorial()
    {
        var equatorial = Compute(new ParkingOrbit(OriginAlt, Inclination: 0));
        var polar      = Compute(new ParkingOrbit(OriginAlt, Inclination: Math.PI / 2));

        // A polar orbit covers the full ecliptic-inclination range of any
        // departure asymptote (|α| ≤ 90°), so deltaI = 0 always.
        // No inclination penalty → ejection Δv ≤ equatorial (law-of-cosines).
        Assert.True(
            polar.EjectionDeltaV <= equatorial.EjectionDeltaV + 0.1,
            $"Polar ejection Δv ({polar.EjectionDeltaV:F2} m/s) should be ≤ " +
            $"equatorial ({equatorial.EjectionDeltaV:F2} m/s)");
    }

    [Fact(DisplayName = "Intermediate (30°) origin orbit: ejection Δv between equatorial and polar")]
    public void IntermediateInclination_EjectionDeltaV_IsBetweenExtremes()
    {
        var equatorial = Compute(new ParkingOrbit(OriginAlt, Inclination: 0));
        var inclined30 = Compute(new ParkingOrbit(OriginAlt, Inclination: 30 * Math.PI / 180));
        var polar      = Compute(new ParkingOrbit(OriginAlt, Inclination: Math.PI / 2));

        // Ejection Δv is monotonically non-increasing as parking-orbit inclination
        // rises from 0 toward 90°.
        Assert.True(
            inclined30.EjectionDeltaV <= equatorial.EjectionDeltaV + 0.1,
            $"30° ejection Δv ({inclined30.EjectionDeltaV:F2} m/s) should be ≤ " +
            $"equatorial ({equatorial.EjectionDeltaV:F2} m/s)");
        Assert.True(
            polar.EjectionDeltaV <= inclined30.EjectionDeltaV + 0.1,
            $"Polar ejection Δv ({polar.EjectionDeltaV:F2} m/s) should be ≤ " +
            $"30° ({inclined30.EjectionDeltaV:F2} m/s)");
    }

    [Fact(DisplayName = "Parking orbit inclination does not affect insertion Δv")]
    public void ParkingOrbitInclination_DoesNotAffectInsertionDeltaV()
    {
        var equatorial = Compute(new ParkingOrbit(OriginAlt, Inclination: 0));
        var polar      = Compute(new ParkingOrbit(OriginAlt, Inclination: Math.PI / 2));

        Assert.Equal(equatorial.InsertionDeltaV, polar.InsertionDeltaV, precision: 1);
    }

    // ---------------------------------------------------------------------------
    // Eccentricity tests
    // ---------------------------------------------------------------------------

    /// <summary>
    /// Using a polar origin orbit (deltaI = 0) to eliminate the inclination term,
    /// so Δv = |v_hyper − v_park|.  The reduction between circular and elliptical
    /// is then purely v_park_peri(e) − v_circ, which is analytically predictable.
    /// </summary>
    [Theory(DisplayName = "Elliptical orbit ejection Δv reduction matches vis-viva")]
    [InlineData(0.3)]
    [InlineData(0.5)]
    public void EllipticalParkingOrbit_EjectionDvReduction_MatchesVisViva(double eccentricity)
    {
        double mu        = Kerbin.GravParam;
        double r         = Kerbin.Radius + OriginAlt;
        double vCirc     = Math.Sqrt(mu / r);
        double vParkPeri = Math.Sqrt(mu * (1 + eccentricity) / r);
        double expectedReduction = vParkPeri - vCirc;

        // Polar origin orbit → deltaI = 0 for both cases; eccentricity isolated.
        var circular   = Compute(new ParkingOrbit(OriginAlt, Inclination: Math.PI / 2));
        var elliptical = Compute(new ParkingOrbit(OriginAlt,
                             Inclination:  Math.PI / 2,
                             Eccentricity: eccentricity));

        double actualReduction = circular.EjectionDeltaV - elliptical.EjectionDeltaV;

        Assert.Equal(expectedReduction, actualReduction, precision: 1); // ±1 m/s
    }

    [Fact(DisplayName = "Elliptical orbit does not affect insertion Δv")]
    public void EllipticalOriginOrbit_DoesNotAffectInsertionDeltaV()
    {
        var circular   = Compute(new ParkingOrbit(OriginAlt));
        var elliptical = Compute(new ParkingOrbit(OriginAlt, Eccentricity: 0.5));

        Assert.Equal(circular.InsertionDeltaV, elliptical.InsertionDeltaV, precision: 1);
    }

    [Theory(DisplayName = "Elliptical destination orbit changes insertion Δv by vis-viva")]
    [InlineData(0.3)]
    [InlineData(0.5)]
    public void EllipticalDestinationOrbit_InsertionDvReduction_MatchesVisViva(double eccentricity)
    {
        double mu        = Duna.GravParam;
        double r         = Duna.Radius + DestAlt;
        double vCirc     = Math.Sqrt(mu / r);
        double vParkPeri = Math.Sqrt(mu * (1 + eccentricity) / r);
        double expectedReduction = vParkPeri - vCirc;

        var circular   = ComputeDest(new ParkingOrbit(DestAlt));
        var elliptical = ComputeDest(new ParkingOrbit(DestAlt, Eccentricity: eccentricity));

        double actualReduction = circular.InsertionDeltaV - elliptical.InsertionDeltaV;

        Assert.Equal(expectedReduction, actualReduction, precision: 1);
    }

    // ---------------------------------------------------------------------------
    // Burn vector tests
    // ---------------------------------------------------------------------------

    [Fact(DisplayName = "Polar parking orbit: ejection burn vector is purely prograde")]
    public void EjectionBurnVector_IsPurePrograde_ForPolarOrbit()
    {
        // Polar orbit: i_park = 90° → deltaI = max(0, |α| − 90°) = 0 always.
        // No plane-change component → pure prograde ejection.
        var result = Compute(new ParkingOrbit(OriginAlt, Inclination: Math.PI / 2));

        Assert.Equal(0.0, result.EjectionBurnVector.Normal,  precision: 1);
        Assert.Equal(0.0, result.EjectionBurnVector.Radial,  precision: 1);
        Assert.True(result.EjectionBurnVector.Prograde > 0,
            "Ejection prograde component should be positive (speed up to escape)");

        double mag = result.EjectionBurnVector.Magnitude;
        Assert.Equal(result.EjectionDeltaV, mag, precision: 1);
    }

    [Fact(DisplayName = "Ejection burn vector magnitude always equals EjectionDeltaV")]
    public void EjectionBurnVector_Magnitude_MatchesEjectionDeltaV()
    {
        // Equatorial orbit: deltaI = |α|, so normal ≠ 0 in general.
        // The law-of-cosines scalar and vector components must be consistent.
        var result = Compute(new ParkingOrbit(OriginAlt, Inclination: 0));

        double mag = result.EjectionBurnVector.Magnitude;
        Assert.Equal(result.EjectionDeltaV, mag, precision: 1);
    }

    [Fact(DisplayName = "Ejection periapsis burn UT is earlier than departure UT")]
    public void EjectionBurnUT_IsEarlierThanDepartureUT()
    {
        var result = Compute(new ParkingOrbit(OriginAlt));

        Assert.True(
            result.EjectionBurnUT < result.DepartureUT,
            $"EjectionBurnUT ({result.EjectionBurnUT:F0} s) should be before " +
            $"DepartureUT ({result.DepartureUT:F0} s)");
    }

    [Fact(DisplayName = "Insertion burn vector is pure retrograde when destination inclination is zero")]
    public void InsertionBurnVector_IsPureRetrograde_WhenIDestIsZero()
    {
        // i_dest = 0 → pure deceleration; prograde < 0 (retrograde), normal = 0.
        var result = ComputeDest(new ParkingOrbit(DestAlt, Inclination: 0));

        Assert.True(result.InsertionBurnVector.Prograde < 0,
            "Insertion prograde should be negative (retrograde capture burn)");
        Assert.Equal(0.0, result.InsertionBurnVector.Normal,  precision: 1);
        Assert.Equal(0.0, result.InsertionBurnVector.Radial,  precision: 1);

        double mag = result.InsertionBurnVector.Magnitude;
        Assert.Equal(result.InsertionDeltaV, mag, precision: 1);
    }

    [Fact(DisplayName = "Insertion burn vector magnitude always equals InsertionDeltaV")]
    public void InsertionBurnVector_Magnitude_MatchesInsertionDeltaV()
    {
        var result = ComputeDest(new ParkingOrbit(DestAlt));

        double mag = result.InsertionBurnVector.Magnitude;
        Assert.Equal(result.InsertionDeltaV, mag, precision: 1);
    }

    [Fact(DisplayName = "Insertion periapsis burn UT is later than arrival UT")]
    public void InsertionBurnUT_IsLaterThanArrivalUT()
    {
        // Spacecraft enters SOI at arrivalUT and coasts to periapsis;
        // the burn happens after SOI entry.
        var result = ComputeDest(new ParkingOrbit(DestAlt));

        Assert.True(
            result.InsertionBurnUT > result.ArrivalUT,
            $"InsertionBurnUT ({result.InsertionBurnUT:F0} s) should be after " +
            $"ArrivalUT ({result.ArrivalUT:F0} s)");
    }

    // ---------------------------------------------------------------------------
    // Helpers
    // ---------------------------------------------------------------------------

    private static TransferResult Compute(ParkingOrbit originOrbit) =>
        TransferComputer.Compute(new TransferParameters(
            Origin:           Kerbin,
            Destination:      Duna,
            DepartureUT:      DepartureUT,
            TimeOfFlight:     Tof,
            OriginOrbit:      originOrbit,
            DestinationOrbit: new ParkingOrbit(DestAlt)
        ));

    private static TransferResult ComputeDest(ParkingOrbit destOrbit) =>
        TransferComputer.Compute(new TransferParameters(
            Origin:           Kerbin,
            Destination:      Duna,
            DepartureUT:      DepartureUT,
            TimeOfFlight:     Tof,
            OriginOrbit:      new ParkingOrbit(OriginAlt),
            DestinationOrbit: destOrbit
        ));
}
