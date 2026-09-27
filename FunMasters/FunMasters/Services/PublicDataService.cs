using FunMasters.Data;
using FunMasters.Shared;
using FunMasters.Shared.DTOs;
using FunMasters.Shared.Services;
using Microsoft.EntityFrameworkCore;

namespace FunMasters.Services;

/// <remarks>
/// Backs the public data page and the CSV beside it. Both read the same <see cref="PublicDataDto"/>,
/// so the export can never drift from what the page shows.
/// </remarks>
public class PublicDataService(ApplicationDbContext db) : IPublicDataApiService
{
    public async Task<PublicDataDto> GetPublicDataAsync()
    {
        var users = await db.Users
            .OrderBy(u => u.CouncilStatus)
            .ThenBy(u => u.UserName)
            .Select(u => new PublicUserRow
            {
                Id = u.Id,
                UserName = u.UserName ?? "",
                CouncilStatus = u.CouncilStatus,
                CycleOrder = u.CycleOrder,
                RegistrationDateUtc = u.RegistrationDateUtc,
                SteamId = u.SteamId
            })
            .ToListAsync();

        // A pending title is still a draft until the Council sees it, and a proposer may hold it
        // back with IsHidden. Everything the Council has actually taken up is public, because the
        // public site already lists it.
        var suggestions = await db.Suggestions
            .Where(s => s.Status != SuggestionStatus.Pending || !s.IsHidden)
            .OrderByDescending(s => s.ActiveAtUtc)
            .ThenBy(s => s.Title)
            .Select(s => new PublicSuggestionRow
            {
                Id = s.Id,
                Title = s.Title,
                Status = s.Status,
                ActiveAtUtc = s.ActiveAtUtc,
                FinishedAtUtc = s.FinishedAtUtc
            })
            .ToListAsync();

        // A verdict is public the moment it is printed on the game's page, review and all.
        var ratings = await db.Ratings
            .OrderByDescending(r => r.CreatedAtUtc)
            .Select(r => new PublicRatingRow
            {
                Id = r.Id,
                SuggestionId = r.SuggestionId,
                SuggestionTitle = r.Suggestion!.Title,
                RaterId = r.RaterId,
                RaterUserName = r.Rater!.UserName,
                Score = r.Score,
                Comment = r.Comment,
                CreatedAtUtc = r.CreatedAtUtc
            })
            .ToListAsync();

        // A row is written for every member who has a Steam account, whether or not Steam ever
        // answered for them - private profiles and unlaunched titles leave an empty row behind. Only
        // rows carrying a figure are worth publishing, and the same test the game page applies.
        var playtimes = await db.SteamPlaytimes
            .Where(sp => sp.PlaytimeForeverMinutes > 0 || sp.Playtime2WeeksMinutes > 0)
            .OrderByDescending(sp => sp.Suggestion!.ActiveAtUtc)
            .ThenBy(sp => sp.Suggestion!.Title)
            .ThenBy(sp => sp.User!.UserName)
            .Select(sp => new PublicPlaytimeRow
            {
                SuggestionId = sp.SuggestionId,
                SuggestionTitle = sp.Suggestion!.Title,
                UserId = sp.UserId,
                UserName = sp.User!.UserName,
                PlaytimeForeverMinutes = sp.PlaytimeForeverMinutes,
                Playtime2WeeksMinutes = sp.Playtime2WeeksMinutes,
                CapturedAtUtc = sp.CapturedAtUtc
            })
            .ToListAsync();

        return new PublicDataDto
        {
            Users = users,
            Suggestions = suggestions,
            Ratings = ratings,
            Playtimes = playtimes
        };
    }
}
