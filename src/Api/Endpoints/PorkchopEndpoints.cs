using KspNavComputer.Api.Dtos;
using KspNavComputer.Api.Mappers;
using KspNavComputer.Core.Bodies;
using KspNavComputer.Core.Transfer;

namespace KspNavComputer.Api.Endpoints;

public static class PorkchopEndpoints
{
    public static void Map(WebApplication app)
    {
        app.MapPost("/api/porkchop", Handle);
    }

    private static IResult Handle(PorkchopRequest req)
    {
        if (!BodyDatabase.All.TryGetValue(req.Origin,      out var origin))
            return Results.BadRequest($"Unknown body: '{req.Origin}'.");
        if (!BodyDatabase.All.TryGetValue(req.Destination, out var destination))
            return Results.BadRequest($"Unknown body: '{req.Destination}'.");

        if (req.EarliestDeparture >= req.LatestDeparture)
            return Results.BadRequest("EarliestDeparture must be before LatestDeparture.");

        if (req.GridCols < 2 || req.GridRows < 2)
            return Results.BadRequest("GridCols and GridRows must be at least 2.");

        if (req.GridCols > 500 || req.GridRows > 500)
            return Results.BadRequest("GridCols and GridRows must not exceed 500.");

        var transferType = req.TransferType switch
        {
            "Ballistic"            => Core.Transfer.TransferType.Ballistic,
            "MidCoursePlaneChange" => Core.Transfer.TransferType.MidCoursePlaneChange,
            _                      => Core.Transfer.TransferType.Optimal,
        };

        ParkingOrbit originOrbit = new(
            req.OriginAltitude,
            req.OriginInclination * Math.PI / 180.0,
            req.OriginEccentricity);

        ParkingOrbit? destinationOrbit = req.NoInsertionBurn ? null : new(
            req.DestinationAltitude,
            req.DestinationInclination * Math.PI / 180.0,
            req.DestinationEccentricity);

        try
        {
            var porkchopParams = new PorkchopParameters(
                Origin:            origin,
                Destination:       destination,
                OriginOrbit:       originOrbit,
                DestinationOrbit:  destinationOrbit,
                EarliestDeparture: req.EarliestDeparture,
                LatestDeparture:   req.LatestDeparture,
                TransferType:      transferType,
                GridCols:          req.GridCols,
                GridRows:          req.GridRows
            );

            var result = PorkchopComputer.Compute(porkchopParams);

            return Results.Ok(new PorkchopResponse(
                DeltaVs:           result.DeltaVs,
                Rows:              result.Rows,
                Cols:              result.Cols,
                EarliestDeparture: result.EarliestDeparture,
                LatestDeparture:   result.LatestDeparture,
                MinTof:            result.MinTof,
                MaxTof:            result.MaxTof,
                MinDeltaV:         result.MinDeltaV,
                MaxDeltaV:         result.MaxDeltaV,
                MeanLogDeltaV:     result.MeanLogDeltaV,
                StdLogDeltaV:      result.StdLogDeltaV,
                OptimalRow:        result.OptimalRow,
                OptimalCol:        result.OptimalCol
            ));
        }
        catch (ArgumentException ex)         { return Results.BadRequest(ex.Message); }
        catch (InvalidOperationException ex) { return Results.BadRequest(ex.Message); }
    }
}
