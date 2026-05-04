using KspNavComputer.Api.Dtos;
using KspNavComputer.Api.Mappers;
using KspNavComputer.Core.Bodies;
using KspNavComputer.Core.Transfer;

namespace KspNavComputer.Api.Endpoints;

public static class TransferEndpoints
{
    public static void Map(WebApplication app)
    {
        app.MapPost("/api/transfer",           HandleOneWay);
        app.MapPost("/api/transfer/roundtrip", HandleRoundTrip);
    }

    private static IResult HandleOneWay(TransferRequest req)
    {
        if (!BodyDatabase.All.TryGetValue(req.Origin,      out var origin))
            return Results.BadRequest($"Unknown body: '{req.Origin}'.");
        if (!BodyDatabase.All.TryGetValue(req.Destination, out var destination))
            return Results.BadRequest($"Unknown body: '{req.Destination}'.");

        try
        {
            var result = TransferComputer.Compute(TransferMapper.ToParameters(req, origin, destination));
            return Results.Ok(TransferMapper.ToResponse(result));
        }
        catch (ArgumentException ex)         { return Results.BadRequest(ex.Message); }
        catch (InvalidOperationException ex) { return Results.BadRequest(ex.Message); }
    }

    private static IResult HandleRoundTrip(RoundTripRequest req)
    {
        if (!BodyDatabase.All.TryGetValue(req.Origin,      out var origin))
            return Results.BadRequest($"Unknown body: '{req.Origin}'.");
        if (!BodyDatabase.All.TryGetValue(req.Destination, out var destination))
            return Results.BadRequest($"Unknown body: '{req.Destination}'.");

        try
        {
            var result = TransferComputer.ComputeRoundTrip(TransferMapper.ToParameters(req, origin, destination));
            return Results.Ok(new RoundTripResponse(
                Outbound:    TransferMapper.ToResponse(result.Outbound),
                Return:      TransferMapper.ToResponse(result.Return),
                TotalDeltaV: result.TotalDeltaV
            ));
        }
        catch (ArgumentException ex)         { return Results.BadRequest(ex.Message); }
        catch (InvalidOperationException ex) { return Results.BadRequest(ex.Message); }
    }
}
