using FunMasters.Shared.DTOs;
using FunMasters.Shared.Services;
using Microsoft.AspNetCore.Mvc;

namespace FunMasters.Endpoints;

public static class LucianEndpoints
{
    public static RouteGroupBuilder MapLucianEndpoints(this IEndpointRouteBuilder routes)
    {
        var group = routes.MapGroup("/api/lucian")
            .DisableAntiforgery();

        // GET /api/lucian/cabinet
        group.MapGet("/cabinet", async (ILucianApiService service) =>
            Results.Ok(await service.GetCabinetAsync()))
            .RequireAuthorization();

        // PUT /api/lucian/triggers/{key}
        group.MapPut("/triggers/{key}", async (
            string key,
            [FromBody] SetLucianTriggerRequest request,
            ILucianApiService service) =>
        {
            var result = await service.SetTriggerEnabledAsync(key, request.Enabled);
            return result.Success ? Results.Json(result) : Results.Json(result, statusCode: 400);
        }).RequireAuthorization("RequireAdmin");

        // POST /api/lucian/alert
        group.MapPost("/alert", async (HttpRequest request, ILucianApiService service) =>
        {
            if (!request.HasFormContentType)
                return Results.Json(ApiResult.Fail("Expected multipart form data"), statusCode: 400);

            var form = await request.ReadFormAsync();
            var text = form["text"].ToString();
            var critical = bool.TryParse(form["critical"], out var c) && c;

            Stream? imageStream = null;
            string? imageFileName = null;
            string? imageContentType = null;
            var file = form.Files.FirstOrDefault();
            if (file is { Length: > 0 })
            {
                imageStream = file.OpenReadStream();
                imageFileName = file.FileName;
                imageContentType = file.ContentType;
            }

            var result = await service.SendAlertAsync(text, critical, imageStream, imageFileName, imageContentType);
            return result.Success ? Results.Json(result) : Results.Json(result, statusCode: 400);
        }).RequireAuthorization();

        return group;
    }
}
