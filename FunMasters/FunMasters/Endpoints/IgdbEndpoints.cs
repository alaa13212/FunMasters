using FunMasters.Shared.Services;

namespace FunMasters.Endpoints;

public static class IgdbEndpoints
{
    public static RouteGroupBuilder MapIgdbEndpoints(this IEndpointRouteBuilder routes)
    {
        var group = routes.MapGroup("/api/igdb")
            .RequireAuthorization()
            .DisableAntiforgery();

        // GET /api/igdb/search?q={query}
        group.MapGet("/search", async (string q, IIgdbApiService service) =>
        {
            if (string.IsNullOrWhiteSpace(q))
                return Results.BadRequest("Query parameter 'q' is required");

            var result = await service.SearchGamesAsync(q);
            return Results.Ok(result);
        });

        return group;
    }
}
