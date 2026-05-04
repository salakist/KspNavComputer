using KspNavComputer.Api.Dtos;
using KspNavComputer.Core.Bodies;

namespace KspNavComputer.Api.Endpoints;

public static class BodiesEndpoints
{
    public static void Map(WebApplication app)
    {
        app.MapGet("/api/bodies", Handle);
    }

    private static BodySummary[] Handle() =>
        BodyDatabase.All.Values
            .Where(b => b.Parent != null)
            .OrderBy(b => b.Name)
            .Select(b => new BodySummary(b.Name, b.Parent!.Name, b.Radius))
            .ToArray();
}
