using KspNavComputer.Api.Dtos;
using KspNavComputer.Core.Bodies;
using KspNavComputer.Core.Time;
using KspNavComputer.Core.Transfer;

namespace KspNavComputer.Api.Endpoints;

public static class TransferEndpoints
{
    public static void Map(WebApplication app)
    {
        app.MapPost("/api/transfer", HandleOneWay);
        app.MapPost("/api/transfer/roundtrip", HandleRoundTrip);
    }

    private static IResult HandleOneWay(TransferRequest req)
    {
        if (!BodyDatabase.All.TryGetValue(req.Origin,      out var origin))
            return Results.BadRequest($"Unknown body: '{req.Origin}'.");
        if (!BodyDatabase.All.TryGetValue(req.Destination, out var destination))
            return Results.BadRequest($"Unknown body: '{req.Destination}'.");

        var parameters = new TransferParameters(
            Origin:           origin,
            Destination:      destination,
            DepartureUT:      req.DepartureUT,
            TimeOfFlight:     req.TimeOfFlight,
            OriginOrbit:      new ParkingOrbit(req.OriginAltitude,
                                  req.OriginInclination * Math.PI / 180.0,
                                  req.OriginEccentricity),
            DestinationOrbit: new ParkingOrbit(req.DestinationAltitude,
                                  req.DestinationInclination * Math.PI / 180.0,
                                  req.DestinationEccentricity)
        );

        TransferResult result;
        try
        {
            result = TransferComputer.Compute(parameters);
        }
        catch (ArgumentException ex)      { return Results.BadRequest(ex.Message); }
        catch (InvalidOperationException ex) { return Results.BadRequest(ex.Message); }

        return Results.Ok(ToResponse(result));
    }

    private static IResult HandleRoundTrip(RoundTripRequest req)
    {
        if (!BodyDatabase.All.TryGetValue(req.Origin,      out var origin))
            return Results.BadRequest($"Unknown body: '{req.Origin}'.");
        if (!BodyDatabase.All.TryGetValue(req.Destination, out var destination))
            return Results.BadRequest($"Unknown body: '{req.Destination}'.");

        var parameters = new RoundTripParameters(
            Origin:               origin,
            Destination:          destination,
            DepartureUT:          req.DepartureUT,
            OutboundTimeOfFlight: req.OutboundTimeOfFlight,
            StayDuration:         req.StayDuration,
            ReturnTimeOfFlight:   req.ReturnTimeOfFlight,
            OriginOrbit:          new ParkingOrbit(req.OriginAltitude,
                                      req.OriginInclination * Math.PI / 180.0,
                                      req.OriginEccentricity),
            DestinationOrbit:     new ParkingOrbit(req.DestinationAltitude,
                                      req.DestinationInclination * Math.PI / 180.0,
                                      req.DestinationEccentricity)
        );

        RoundTripResult result;
        try
        {
            result = TransferComputer.ComputeRoundTrip(parameters);
        }
        catch (ArgumentException ex)      { return Results.BadRequest(ex.Message); }
        catch (InvalidOperationException ex) { return Results.BadRequest(ex.Message); }

        return Results.Ok(new RoundTripResponse(
            Outbound:    ToResponse(result.Outbound),
            Return:      ToResponse(result.Return),
            TotalDeltaV: result.TotalDeltaV
        ));
    }

    private static TransferResponse ToResponse(TransferResult r) => new(
        DepartureUT:     r.DepartureUT,
        DepartureDate:   KspTime.Format(r.DepartureUT),
        ArrivalUT:       r.ArrivalUT,
        ArrivalDate:     KspTime.Format(r.ArrivalUT),
        EjectionDeltaV:  r.EjectionDeltaV,
        InsertionDeltaV: r.InsertionDeltaV,
        TotalDeltaV:     r.TotalDeltaV
    );
}
