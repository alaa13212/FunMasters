using FunMasters.Shared.DTOs;
using FunMasters.Shared.Services;
using Microsoft.AspNetCore.Mvc;

namespace FunMasters.Endpoints;

public static class RatingEndpoints
{
    public static RouteGroupBuilder MapRatingEndpoints(this IEndpointRouteBuilder routes)
    {
        var group = routes.MapGroup("/api/ratings")
            .DisableAntiforgery();

        // GET /api/ratings/unrated
        group.MapGet("/unrated", async (IRatingApiService service) =>
        {
            var result = await service.GetUnratedSuggestionsAsync();
            return Results.Ok(result);
        }).RequireAuthorization();

        // GET /api/ratings/my
        group.MapGet("/my", async (IRatingApiService service) =>
        {
            var result = await service.GetMyRatingsAsync();
            return Results.Ok(result);
        }).RequireAuthorization();

        // POST /api/ratings
        group.MapPost("/", async (
            [FromBody] CreateRatingRequest request,
            IRatingApiService service) =>
        {
            var result = await service.CreateRatingAsync(request);
            return Results.Json(result);
        }).RequireAuthorization();

        // PUT /api/ratings/{id}
        group.MapPut("/{id:guid}", async (
            Guid id,
            [FromBody] UpdateRatingRequest request,
            IRatingApiService service) =>
        {
            var result = await service.UpdateRatingAsync(id, request);
            return result.Success
                ? Results.Json(result)
                : Results.Json(result, statusCode: result.ErrorMessage?.Contains("not found") == true ? 404 : 403);
        }).RequireAuthorization();

        return group;
    }
}
