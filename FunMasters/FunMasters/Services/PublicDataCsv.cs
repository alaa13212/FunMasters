using System.Globalization;
using System.Text;
using FunMasters.Shared.DTOs;

namespace FunMasters.Services;

/// <summary>
/// Renders a <see cref="PublicDataDto"/> as CSV, one table per file.
/// </summary>
/// <remarks>
/// Built from the same DTO the page renders, so the download and the page can never disagree.
/// Dates go out as ISO 8601 UTC: the only form a spreadsheet, a spreadsheet re-export, and a parser
/// all read the same way.
/// </remarks>
public static class PublicDataCsv
{
    public static (string FileName, string Content)? Build(PublicDataDto data, string kind) => kind switch
    {
        "users" => ("funmasters-users.csv", Render(
            ["Id", "UserName", "CouncilStatus", "CycleOrder", "RegistrationDateUtc", "SteamId"],
            data.Users.Select(u => new object?[]
            {
                u.Id, u.UserName, u.CouncilStatus, u.CycleOrder, u.RegistrationDateUtc, u.SteamId
            }))),

        "suggestions" => ("funmasters-suggestions.csv", Render(
            ["Id", "Title", "Status", "ActiveAtUtc", "FinishedAtUtc"],
            data.Suggestions.Select(s => new object?[]
            {
                s.Id, s.Title, s.Status, s.ActiveAtUtc, s.FinishedAtUtc
            }))),

        "ratings" => ("funmasters-ratings.csv", Render(
            ["Id", "SuggestionId", "SuggestionTitle", "RaterId", "RaterUserName",
                "Score", "Comment", "CreatedAtUtc"],
            data.Ratings.Select(r => new object?[]
            {
                r.Id, r.SuggestionId, r.SuggestionTitle, r.RaterId, r.RaterUserName,
                r.Score, r.Comment, r.CreatedAtUtc
            }))),

        "playtimes" => ("funmasters-playtimes.csv", Render(
            ["SuggestionId", "SuggestionTitle", "UserId", "UserName",
                "PlaytimeForeverMinutes", "Playtime2WeeksMinutes", "CapturedAtUtc"],
            data.Playtimes.Select(p => new object?[]
            {
                p.SuggestionId, p.SuggestionTitle, p.UserId, p.UserName,
                p.PlaytimeForeverMinutes, p.Playtime2WeeksMinutes, p.CapturedAtUtc
            }))),

        _ => null
    };

    private static string Render(string[] header, IEnumerable<object?[]> rows)
    {
        var sb = new StringBuilder();
        sb.AppendLine(string.Join(',', header.Select(Field)));

        foreach (var row in rows)
            sb.AppendLine(string.Join(',', row.Select(Cell)));

        return sb.ToString();
    }

    /// <summary>
    /// A cell, flattened for CSV. Nullable dates are not a case here: boxing a null
    /// <see cref="DateTime"/> into an object array yields null, which the first arm already takes.
    /// </summary>
    private static string Cell(object? value) => value switch
    {
        null => "",
        bool b => b ? "true" : "false",
        DateTime dt => dt.ToUniversalTime().ToString("yyyy-MM-ddTHH:mm:ss'Z'", CultureInfo.InvariantCulture),
        int n => n.ToString(CultureInfo.InvariantCulture),
        Guid g => g.ToString(),
        _ => Convert.ToString(value, CultureInfo.InvariantCulture) ?? ""
    };

    /// <summary>Quotes only what has to be quoted - reviews carry commas and line breaks.</summary>
    private static string Field(string value) =>
        value.AsSpan().IndexOfAny(",\"\n\r") < 0
            ? value
            : '"' + value.Replace("\"", "\"\"") + '"';
}
