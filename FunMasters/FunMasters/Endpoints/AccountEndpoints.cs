using FunMasters.Shared.DTOs;
using FunMasters.Shared.Services;
using Microsoft.AspNetCore.Mvc;

namespace FunMasters.Endpoints;

public static class AccountEndpoints
{
    public static RouteGroupBuilder MapAccountEndpoints(this IEndpointRouteBuilder routes)
    {
        var group = routes.MapGroup("/api/account")
            .DisableAntiforgery();

        // POST /api/account/login
        group.MapPost("/login", async (
            [FromBody] LoginRequest request,
            IAccountApiService service) =>
        {
            var result = await service.LoginAsync(request);
            return Results.Json(result);
        });

        // POST /api/account/logout
        group.MapPost("/logout", async (IAccountApiService service) =>
        {
            await service.LogoutAsync();
            return Results.Ok();
        }).RequireAuthorization();

        // GET /api/account/profile
        group.MapGet("/profile", async (IAccountApiService service) =>
        {
            var result = await service.GetProfileAsync();
            return result != null ? Results.Ok(result) : Results.NotFound();
        }).RequireAuthorization();

        // PUT /api/account/profile
        group.MapPut("/profile", async (
            [FromBody] UpdateProfileRequest request,
            IAccountApiService service) =>
        {
            var result = await service.UpdateProfileAsync(request);
            return result.Success
                ? Results.Json(result)
                : Results.Json(result, statusCode: 400);
        }).RequireAuthorization();

        // POST /api/account/change-password
        group.MapPost("/change-password", async (
            [FromBody] ChangePasswordRequest request,
            IAccountApiService service) =>
        {
            var result = await service.ChangePasswordAsync(request);
            return result.Success
                ? Results.Json(result)
                : Results.Json(result, statusCode: 400);
        }).RequireAuthorization();

        // POST /api/account/upload-avatar
        group.MapPost("/upload-avatar", async (
            HttpRequest request,
            IAccountApiService service) =>
        {
            if (!request.HasFormContentType || request.Form.Files.Count == 0)
                return Results.Json(ApiResult<string>.Fail("No file uploaded"), statusCode: 400);

            var file = request.Form.Files[0];
            if (file.Length == 0)
                return Results.Json(ApiResult<string>.Fail("Empty file"), statusCode: 400);

            await using var stream = file.OpenReadStream();
            var result = await service.UploadAvatarAsync(stream, file.FileName);

            return result.Success
                ? Results.Json(result)
                : Results.Json(result, statusCode: 400);
        }).RequireAuthorization();

        return group;
    }
}
