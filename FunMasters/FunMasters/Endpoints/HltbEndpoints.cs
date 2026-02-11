using FunMasters.Shared.Services;

namespace FunMasters.Endpoints;

public static class HltbEndpoints
{
    public static RouteGroupBuilder MapHltbEndpoints(this IEndpointRouteBuilder routes)
    {
        var group = routes.MapGroup("/api/hltb")
            .DisableAntiforgery();

        // GET /api/hltb/search?name={name}
        group.MapGet("/search", async (string name, IHltbApiService service) =>
        {
            if (string.IsNullOrWhiteSpace(name))
                return Results.BadRequest("Query parameter 'name' is required");

            var result = await service.SearchGameAsync(name);
            return result != null ? Results.Ok(result) : Results.NotFound();
        });

        return group;
    }
}
