using FunMasters.Shared.DTOs;
using FunMasters.Shared.Services;
using Microsoft.AspNetCore.Mvc;

namespace FunMasters.Endpoints;

public static class SuggestionEndpoints
{
    public static RouteGroupBuilder MapSuggestionEndpoints(this IEndpointRouteBuilder routes)
    {
        var group = routes.MapGroup("/api/suggestions")
            .DisableAntiforgery();

        // GET /api/suggestions/home
        group.MapGet("/home", async (ISuggestionApiService service) =>
        {
            var result = await service.GetHomeDataAsync();
            return Results.Ok(result);
        });

        // GET /api/suggestions/floating
        group.MapGet("/floating", async (ISuggestionApiService service) =>
        {
            var result = await service.GetFloatingSuggestionsAsync();
            return Results.Ok(result);
        });

        // GET /api/suggestions/{id}
        group.MapGet("/{id:guid}", async (Guid id, ISuggestionApiService service) =>
        {
            var result = await service.GetSuggestionDetailsAsync(id);
            return result != null ? Results.Ok(result) : Results.NotFound();
        });

        // GET /api/suggestions/active
        group.MapGet("/active", async (ISuggestionApiService service) =>
        {
            var result = await service.GetActiveSuggestionAsync();
            return result != null ? Results.Ok(result) : Results.NotFound();
        });

        // GET /api/suggestions/my
        group.MapGet("/my", async (ISuggestionApiService service) =>
        {
            var result = await service.GetMySuggestionsAsync();
            return Results.Ok(result);
        }).RequireAuthorization();
        
        // GET /api/suggestions/my
        group.MapGet("/my/{id:guid}", async (Guid id, ISuggestionApiService service) =>
        {
            var result = await service.GetMySuggestionAsync(id);
            return Results.Ok(result);
        }).RequireAuthorization();

        // GET /api/suggestions/my/next-order
        group.MapGet("/my/next-order", async (ISuggestionApiService service) =>
        {
            var result = await service.GetNextOrderAsync();
            return Results.Ok(result);
        }).RequireAuthorization();

        // POST /api/suggestions
        group.MapPost("/", async (
            [FromBody] CreateSuggestionRequest request,
            ISuggestionApiService service) =>
        {
            var result = await service.CreateSuggestionAsync(request);
            return Results.Json(result);
        }).RequireAuthorization();

        // PUT /api/suggestions/{id}
        group.MapPut("/{id:guid}", async (
            Guid id,
            [FromBody] UpdateSuggestionRequest request,
            ISuggestionApiService service) =>
        {
            var result = await service.UpdateSuggestionAsync(id, request);
            return result.Success
                ? Results.Json(result)
                : Results.Json(result, statusCode: result.ErrorMessage?.Contains("not found") == true ? 404 : 403);
        }).RequireAuthorization();

        // DELETE /api/suggestions/{id}
        group.MapDelete("/{id:guid}", async (
            Guid id,
            ISuggestionApiService service) =>
        {
            var result = await service.DeleteSuggestionAsync(id);
            return result.Success
                ? Results.Json(result)
                : Results.Json(result, statusCode: result.ErrorMessage?.Contains("not found") == true ? 404 : 403);
        }).RequireAuthorization();

        // POST /api/suggestions/reorder
        group.MapPost("/reorder", async (
            [FromBody] ReorderSuggestionRequest request,
            ISuggestionApiService service) =>
        {
            var result = await service.ReorderSuggestionAsync(request);
            return Results.Json(result);
        }).RequireAuthorization();

        return group;
    }
}
