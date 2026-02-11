using FunMasters.Shared.DTOs;
using FunMasters.Shared.Services;
using Microsoft.AspNetCore.Mvc;

namespace FunMasters.Endpoints;

public static class AdminEndpoints
{
    public static RouteGroupBuilder MapAdminEndpoints(this IEndpointRouteBuilder routes)
    {
        var group = routes.MapGroup("/api/admin")
            .RequireAuthorization("RequireAdmin")
            .DisableAntiforgery();

        // User management
        // GET /api/admin/users
        group.MapGet("/users", async (IAdminApiService service) =>
        {
            var result = await service.GetUsersAsync();
            return Results.Ok(result);
        });

        // GET /api/admin/users/{id}
        group.MapGet("/users/{id:guid}", async (Guid id, IAdminApiService service) =>
        {
            var result = await service.GetUserAsync(id);
            return result != null ? Results.Ok(result) : Results.NotFound();
        });

        // POST /api/admin/users
        group.MapPost("/users", async (
            [FromBody] CreateUserRequest request,
            IAdminApiService service) =>
        {
            var result = await service.CreateUserAsync(request);
            return Results.Json(result);
        });

        // PUT /api/admin/users/{id}
        group.MapPut("/users/{id:guid}", async (
            Guid id,
            [FromBody] UpdateUserRequest request,
            IAdminApiService service) =>
        {
            var result = await service.UpdateUserAsync(id, request);
            return result.Success
                ? Results.Json(result)
                : Results.Json(result, statusCode: result.ErrorMessage?.Contains("not found") == true ? 404 : 400);
        });

        // DELETE /api/admin/users/{id}
        group.MapDelete("/users/{id:guid}", async (
            Guid id,
            IAdminApiService service) =>
        {
            var result = await service.DeleteUserAsync(id);
            return result.Success
                ? Results.Json(result)
                : Results.Json(result, statusCode: result.ErrorMessage?.Contains("not found") == true ? 404 : 400);
        });

        // POST /api/admin/users/{id}/change-password
        group.MapPost("/users/{id:guid}/change-password", async (
            Guid id,
            [FromBody] AdminChangePasswordRequest request,
            IAdminApiService service) =>
        {
            var result = await service.ChangeUserPasswordAsync(id, request);
            return result.Success
                ? Results.Json(result)
                : Results.Json(result, statusCode: result.ErrorMessage?.Contains("not found") == true ? 404 : 400);
        });

        // Suggestion management
        // GET /api/admin/suggestions
        group.MapGet("/suggestions", async (IAdminApiService service) =>
        {
            var result = await service.GetAllSuggestionsAsync();
            return Results.Ok(result);
        });

        // PUT /api/admin/suggestions/{id}
        group.MapPut("/suggestions/{id:guid}", async (
            Guid id,
            [FromBody] AdminUpdateSuggestionRequest request,
            IAdminApiService service) =>
        {
            var result = await service.UpdateSuggestionAsync(id, request);
            return result.Success
                ? Results.Json(result)
                : Results.Json(result, statusCode: result.ErrorMessage?.Contains("not found") == true ? 404 : 400);
        });

        // Queue management
        // POST /api/admin/queue/refresh
        group.MapPost("/queue/refresh", async (IAdminApiService service) =>
        {
            var result = await service.RefreshQueueAsync();
            return Results.Json(result);
        });

        return group;
    }
}
