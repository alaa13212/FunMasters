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

        // POST /api/admin/suggestions/{id}/finish-early
        group.MapPost("/suggestions/{id:guid}/finish-early", async (
            Guid id,
            IAdminApiService service) =>
        {
            var result = await service.FinishEarlyAsync(id);
            return result.Success
                ? Results.Json(result)
                : Results.Json(result, statusCode: result.ErrorMessage?.Contains("not found") == true ? 404 : 400);
        });

        // Badge management
        // GET /api/admin/badges
        group.MapGet("/badges", async (IAdminApiService service) =>
        {
            var result = await service.GetBadgesAsync();
            return Results.Ok(result);
        });

        // POST /api/admin/badges
        group.MapPost("/badges", async (
            HttpRequest request,
            IAdminApiService service) =>
        {
            var form = await request.ReadFormAsync();
            var name = form["name"].ToString();
            var description = form["description"].ToString();
            var file = form.Files.FirstOrDefault();

            Stream? fileStream = null;
            string? fileName = null;
            if (file is { Length: > 0 })
            {
                fileStream = file.OpenReadStream();
                fileName = file.FileName;
            }

            var result = await service.CreateBadgeAsync(name, description, fileStream, fileName);
            return Results.Json(result);
        });

        // PUT /api/admin/badges/{id}
        group.MapPut("/badges/{id:guid}", async (
            Guid id,
            [FromBody] UpdateBadgeRequest request,
            IAdminApiService service) =>
        {
            var result = await service.UpdateBadgeAsync(id, request);
            return result.Success
                ? Results.Json(result)
                : Results.Json(result, statusCode: 400);
        });

        // DELETE /api/admin/badges/{id}
        group.MapDelete("/badges/{id:guid}", async (
            Guid id,
            IAdminApiService service) =>
        {
            var result = await service.DeleteBadgeAsync(id);
            return result.Success
                ? Results.Json(result)
                : Results.Json(result, statusCode: 400);
        });

        // POST /api/admin/badges/{id}/image
        group.MapPost("/badges/{id:guid}/image", async (
            Guid id,
            HttpRequest request,
            IAdminApiService service) =>
        {
            if (!request.HasFormContentType || request.Form.Files.Count == 0)
                return Results.Json(ApiResult.Fail("No file uploaded"), statusCode: 400);

            var file = request.Form.Files[0];
            if (file.Length == 0)
                return Results.Json(ApiResult.Fail("Empty file"), statusCode: 400);

            await using var stream = file.OpenReadStream();
            var result = await service.UploadBadgeImageAsync(id, stream, file.FileName);

            return result.Success
                ? Results.Json(result)
                : Results.Json(result, statusCode: 400);
        });

        // POST /api/admin/users/{userId}/badges/{badgeId}
        group.MapPost("/users/{userId:guid}/badges/{badgeId:guid}", async (
            Guid userId,
            Guid badgeId,
            IAdminApiService service) =>
        {
            var result = await service.AssignBadgeAsync(userId, badgeId);
            return result.Success
                ? Results.Json(result)
                : Results.Json(result, statusCode: 400);
        });

        // DELETE /api/admin/users/{userId}/badges/{badgeId}
        group.MapDelete("/users/{userId:guid}/badges/{badgeId:guid}", async (
            Guid userId,
            Guid badgeId,
            IAdminApiService service) =>
        {
            var result = await service.RemoveBadgeAsync(userId, badgeId);
            return result.Success
                ? Results.Json(result)
                : Results.Json(result, statusCode: 400);
        });

        return group;
    }
}
