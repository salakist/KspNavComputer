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
            DestinationOrbit: ToParkingOrbit(req.DestinationAltitude, req.DestinationInclination, req.DestinationEccentricity)
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
        DepartureUT:     r.DepartureUT,
        DepartureDate:   KspTime.Format(r.DepartureUT),
        ArrivalUT:       r.ArrivalUT,
        ArrivalDate:     KspTime.Format(r.ArrivalUT),
        EjectionDeltaV:  r.EjectionDeltaV,
        InsertionDeltaV: r.InsertionDeltaV,
        TotalDeltaV:     r.TotalDeltaV
    );

    // Inclination is stored in degrees in DTOs; Core uses radians.
    private static ParkingOrbit ToParkingOrbit(
        double altitudeM, double inclinationDeg, double eccentricity) =>
        new(altitudeM, inclinationDeg * Math.PI / 180.0, eccentricity);
}
