using FunMasters.Shared.DTOs;
using FunMasters.Shared.Services;
using Microsoft.AspNetCore.Mvc;

namespace FunMasters.Endpoints;

public static class SteamEndpoints
{
    public static RouteGroupBuilder MapSteamEndpoints(this IEndpointRouteBuilder routes)
    {
        var group = routes.MapGroup("/api/steam")
            .DisableAntiforgery()
            .RequireAuthorization();

        group.MapPost("/refresh-playtimes/{suggestionId:guid}", async (
            Guid suggestionId,
            ISteamApiService service) =>
        {
            var result = await service.RefreshPlaytimesAsync(suggestionId);
            return Results.Json(result);
        });

        // POST /api/steam/resolve
        group.MapPost("/resolve", async (
            [FromBody] SteamResolveRequest request,
            ISteamApiService service) =>
        {
            var result = await service.ResolveSteamIdAsync(request.Input);
            return result.Success
                ? Results.Json(result)
                : Results.Json(result, statusCode: 400);
        });

        return group;
    }
}
