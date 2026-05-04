using KspNavComputer.Core.Bodies;
using KspNavComputer.Core.Time;
using KspNavComputer.Core.Transfer;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
        policy.WithOrigins("http://localhost:5173", "http://127.0.0.1:5173")
              .AllowAnyHeader()
              .AllowAnyMethod());
});

var app = builder.Build();

app.UseCors();

// ---- POST /api/transfer ----
app.MapPost("/api/transfer", (TransferRequest req) =>
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
        OriginOrbit:      new ParkingOrbit(req.OriginAltitude),
        DestinationOrbit: new ParkingOrbit(req.DestinationAltitude)
    );

    TransferResult result;
    try
    {
        result = TransferComputer.Compute(parameters);
    }
    catch (ArgumentException ex)
    {
        return Results.BadRequest(ex.Message);
    }
    catch (InvalidOperationException ex)
    {
        return Results.BadRequest(ex.Message);
    }

    return Results.Ok(new TransferResponse(
        DepartureUT:        result.DepartureUT,
        DepartureDate:      KspTime.Format(result.DepartureUT),
        ArrivalUT:          result.ArrivalUT,
        ArrivalDate:        KspTime.Format(result.ArrivalUT),
        EjectionDeltaV:     result.EjectionDeltaV,
        InsertionDeltaV:    result.InsertionDeltaV,
        TotalDeltaV:        result.TotalDeltaV
    ));
});

// ---- GET /api/bodies ----
app.MapGet("/api/bodies", () =>
    BodyDatabase.All.Values
        .Where(b => b.Parent != null)          // exclude Kerbol itself
        .OrderBy(b => b.Name)
        .Select(b => new BodySummary(b.Name, b.Parent!.Name, b.Radius))
        .ToArray()
);

// ---- POST /api/transfer/roundtrip ----
app.MapPost("/api/transfer/roundtrip", (RoundTripRequest req) =>
{
    if (!BodyDatabase.All.TryGetValue(req.Origin,      out var origin))
        return Results.BadRequest($"Unknown body: '{req.Origin}'.");
    if (!BodyDatabase.All.TryGetValue(req.Destination, out var destination))
        return Results.BadRequest($"Unknown body: '{req.Destination}'.");

    var parameters = new RoundTripParameters(
        Origin:                 origin,
        Destination:            destination,
        DepartureUT:            req.DepartureUT,
        OutboundTimeOfFlight:   req.OutboundTimeOfFlight,
        StayDuration:           req.StayDuration,
        ReturnTimeOfFlight:     req.ReturnTimeOfFlight,
        OriginOrbit:            new ParkingOrbit(req.OriginAltitude),
        DestinationOrbit:       new ParkingOrbit(req.DestinationAltitude)
    );

    RoundTripResult result;
    try
    {
        result = TransferComputer.ComputeRoundTrip(parameters);
    }
    catch (ArgumentException ex)
    {
        return Results.BadRequest(ex.Message);
    }
    catch (InvalidOperationException ex)
    {
        return Results.BadRequest(ex.Message);
    }

    return Results.Ok(new RoundTripResponse(
        Outbound: new TransferResponse(
            DepartureUT:     result.Outbound.DepartureUT,
            DepartureDate:   KspTime.Format(result.Outbound.DepartureUT),
            ArrivalUT:       result.Outbound.ArrivalUT,
            ArrivalDate:     KspTime.Format(result.Outbound.ArrivalUT),
            EjectionDeltaV:  result.Outbound.EjectionDeltaV,
            InsertionDeltaV: result.Outbound.InsertionDeltaV,
            TotalDeltaV:     result.Outbound.TotalDeltaV
        ),
        Return: new TransferResponse(
            DepartureUT:     result.Return.DepartureUT,
            DepartureDate:   KspTime.Format(result.Return.DepartureUT),
            ArrivalUT:       result.Return.ArrivalUT,
            ArrivalDate:     KspTime.Format(result.Return.ArrivalUT),
            EjectionDeltaV:  result.Return.EjectionDeltaV,
            InsertionDeltaV: result.Return.InsertionDeltaV,
            TotalDeltaV:     result.Return.TotalDeltaV
        ),
        TotalDeltaV: result.TotalDeltaV
    ));
});

app.Run();

// ---- DTOs ----
record TransferRequest(
    string Origin,
    string Destination,
    double DepartureUT,
    double TimeOfFlight,
    double OriginAltitude,
    double DestinationAltitude
);

record TransferResponse(
    double DepartureUT,
    string DepartureDate,
    double ArrivalUT,
    string ArrivalDate,
    double EjectionDeltaV,
    double InsertionDeltaV,
    double TotalDeltaV
);

record BodySummary(string Name, string Parent, double Radius);

record RoundTripRequest(
    string Origin,
    string Destination,
    double DepartureUT,
    double OutboundTimeOfFlight,
    double StayDuration,
    double ReturnTimeOfFlight,
    double OriginAltitude,
    double DestinationAltitude
);

record RoundTripResponse(
    TransferResponse Outbound,
    TransferResponse Return,
    double TotalDeltaV
);
