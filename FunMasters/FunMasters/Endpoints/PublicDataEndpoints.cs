using System.Text;
using FunMasters.Services;
using FunMasters.Shared.Services;

namespace FunMasters.Endpoints;

public static class PublicDataEndpoints
{
    public static RouteGroupBuilder MapPublicDataEndpoints(this IEndpointRouteBuilder routes)
    {
        var group = routes.MapGroup("/api/public-data")
            .DisableAntiforgery();

        // Public, like the roll and the profiles it mirrors. Carries no email addresses.
        group.MapGet("/", async (IPublicDataApiService service) =>
        {
            var data = await service.GetPublicDataAsync();
            return Results.Ok(data);
        });

        // One file per table, so each opens in a spreadsheet without a parser having to skip the
        // other three. The BOM is deliberate: without it Excel reads UTF-8 reviews as mojibake.
        group.MapGet("/export/{kind}.csv", async (string kind, IPublicDataApiService service) =>
        {
            var data = await service.GetPublicDataAsync();
            var export = PublicDataCsv.Build(data, kind);

            if (export == null)
                return Results.NotFound();

            // The BOM is deliberate: without it Excel reads UTF-8 reviews as mojibake. It has to be
            // concatenated on by hand - UTF8Encoding.GetBytes does not write it for you.
            var encoding = new UTF8Encoding(encoderShouldEmitUTF8Identifier: true);
            var bytes = encoding.GetPreamble().Concat(encoding.GetBytes(export.Value.Content)).ToArray();
            return Results.File(bytes, "text/csv; charset=utf-8", export.Value.FileName);
        });

        return group;
    }
}
