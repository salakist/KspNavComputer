using KspNavComputer.Api.Dtos;
using KspNavComputer.Core.Bodies;
using KspNavComputer.Core.Time;
using KspNavComputer.Core.Transfer;

namespace KspNavComputer.Api.Mappers;

internal static class TransferMapper
{
    internal static TransferParameters ToParameters(
        TransferRequest req, CelestialBody origin, CelestialBody destination) =>
        new(
            Origin:           origin,
            Destination:      destination,
            DepartureUT:      req.DepartureUT,
            TimeOfFlight:     req.TimeOfFlight,
            OriginOrbit:      ToParkingOrbit(req.OriginAltitude,      req.OriginInclination,      req.OriginEccentricity),
            DestinationOrbit: ToParkingOrbit(req.DestinationAltitude, req.DestinationInclination, req.DestinationEccentricity),
            TransferType:     ParseTransferType(req.TransferType),
            NoInsertionBurn:  req.NoInsertionBurn
        );

    internal static RoundTripParameters ToParameters(
        RoundTripRequest req, CelestialBody origin, CelestialBody destination) =>
        new(
            Origin:               origin,
            Destination:          destination,
            DepartureUT:          req.DepartureUT,
            OutboundTimeOfFlight: req.OutboundTimeOfFlight,
            StayDuration:         req.StayDuration,
            ReturnTimeOfFlight:   req.ReturnTimeOfFlight,
            OriginOrbit:          ToParkingOrbit(req.OriginAltitude,      req.OriginInclination,      req.OriginEccentricity),
            DestinationOrbit:     ToParkingOrbit(req.DestinationAltitude, req.DestinationInclination, req.DestinationEccentricity)
        );

    internal static TransferResponse ToResponse(TransferResult r) => new(
        DepartureUT:             r.DepartureUT,
        DepartureDate:           KspTime.Format(r.DepartureUT),
        ArrivalUT:               r.ArrivalUT,
        ArrivalDate:             KspTime.Format(r.ArrivalUT),
        Ejection:                ToBurnDto(r.Ejection),
        Insertion:               ToBurnDto(r.Insertion),
        TotalDeltaV:             r.TotalDeltaV,
        PlaneChange:             r.PlaneChange is null ? null : ToPlaneChangeBurnDto(r.PlaneChange),
        PhaseAngleDeg:           r.PhaseAngleDeg,
        TransferAngleDeg:        r.TransferAngleDeg,
        TransferPeriapsis:       r.TransferPeriapsis,
        TransferApoapsis:        r.TransferApoapsis,
        InsertionInclinationDeg: r.InsertionInclinationDeg
    );

    private static BurnDto ToBurnDto(Burn b) => new(
        DeltaV:               b.DeltaV,
        BurnUT:               b.BurnUT,
        BurnDate:             KspTime.Format(b.BurnUT),
        Vector:               new BurnVectorDto(b.Vector.Prograde, b.Vector.Normal, b.Vector.Radial),
        PreciseManeuverText:  PreciseManeuverFormatter.Format(b),
        EjectionDetails:      b.Ejection is null ? null
                              : new EjectionDetailsDto(b.Ejection.AngleDeg, b.Ejection.InclinationDeg)
    );

    private static PlaneChangeBurnDto ToPlaneChangeBurnDto(Burn pc) => new(
        DeltaV:              pc.DeltaV,
        BurnUT:              pc.BurnUT,
        BurnDate:            KspTime.Format(pc.BurnUT),
        Vector:              new BurnVectorDto(pc.Vector.Prograde, pc.Vector.Normal, pc.Vector.Radial),
        PreciseManeuverText: PreciseManeuverFormatter.Format(pc)
    );

    // Inclination is stored in degrees in DTOs; Core uses radians.
    private static ParkingOrbit ToParkingOrbit(
        double altitudeM, double inclinationDeg, double eccentricity) =>
        new(altitudeM, inclinationDeg * Math.PI / 180.0, eccentricity);

    private static Core.Transfer.TransferType ParseTransferType(string s) =>
        s switch
        {
            "Ballistic"            => Core.Transfer.TransferType.Ballistic,
            "MidCoursePlaneChange" => Core.Transfer.TransferType.MidCoursePlaneChange,
            _                      => Core.Transfer.TransferType.Optimal,
        };
}

