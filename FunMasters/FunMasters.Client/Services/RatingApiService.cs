using System.Net.Http.Json;
using FunMasters.Shared.DTOs;
using FunMasters.Shared.Services;

namespace FunMasters.Client.Services;

public class RatingApiService(HttpClient http) : IRatingApiService
{
    public async Task<List<SuggestionDto>> GetUnratedSuggestionsAsync()
    {
        return await http.GetFromJsonAsync<List<SuggestionDto>>("/api/ratings/unrated")
            ?? [];
    }

    public async Task<List<UserRatingDto>> GetMyRatingsAsync()
    {
        return await http.GetFromJsonAsync<List<UserRatingDto>>("/api/ratings/my")
            ?? [];
    }

    public async Task<ApiResult<Guid>> CreateRatingAsync(CreateRatingRequest request)
    {
        var response = await http.PostAsJsonAsync("/api/ratings", request);
        return await response.Content.ReadFromJsonAsync<ApiResult<Guid>>()
            ?? ApiResult<Guid>.Fail("Failed to create rating");
    }

    public async Task<ApiResult> UpdateRatingAsync(Guid id, UpdateRatingRequest request)
    {
        var response = await http.PutAsJsonAsync($"/api/ratings/{id}", request);
        return await response.Content.ReadFromJsonAsync<ApiResult>()
            ?? ApiResult.Fail("Failed to update rating");
    }
}
