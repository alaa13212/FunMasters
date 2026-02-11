using System.Net.Http.Json;
using FunMasters.Shared.DTOs;
using FunMasters.Shared.Services;

namespace FunMasters.Client.Services;

public class SuggestionApiService(HttpClient http) : ISuggestionApiService
{
    public async Task<HomePageDto> GetHomeDataAsync()
    {
        return await http.GetFromJsonAsync<HomePageDto>("/api/suggestions/home")
            ?? throw new Exception("Failed to load home data");
    }

    public async Task<List<SuggestionDto>> GetFloatingSuggestionsAsync()
    {
        return await http.GetFromJsonAsync<List<SuggestionDto>>("/api/suggestions/floating")
            ?? [];
    }

    public async Task<SuggestionDetailDto?> GetSuggestionDetailsAsync(Guid id)
    {
        return await http.GetFromJsonAsync<SuggestionDetailDto>($"/api/suggestions/{id}");
    }

    public async Task<SuggestionDetailDto?> GetActiveSuggestionAsync()
    {
        return await http.GetFromJsonAsync<SuggestionDetailDto>("/api/suggestions/active");
    }

    public async Task<List<SuggestionDto>> GetMySuggestionsAsync()
    {
        return await http.GetFromJsonAsync<List<SuggestionDto>>("/api/suggestions/my")
            ?? [];
    }
    
    public async Task<SuggestionDto?> GetMySuggestionAsync(Guid id)
    {
        return await http.GetFromJsonAsync<SuggestionDto>($"/api/suggestions/my/{id}");
    }

    public async Task<int> GetNextOrderAsync()
    {
        return await http.GetFromJsonAsync<int>("/api/suggestions/my/next-order");
    }

    public async Task<ApiResult<Guid>> CreateSuggestionAsync(CreateSuggestionRequest request)
    {
        var response = await http.PostAsJsonAsync("/api/suggestions", request);
        return await response.Content.ReadFromJsonAsync<ApiResult<Guid>>()
            ?? ApiResult<Guid>.Fail("Failed to create suggestion");
    }

    public async Task<ApiResult> UpdateSuggestionAsync(Guid id, UpdateSuggestionRequest request)
    {
        var response = await http.PutAsJsonAsync($"/api/suggestions/{id}", request);
        return await response.Content.ReadFromJsonAsync<ApiResult>()
            ?? ApiResult.Fail("Failed to update suggestion");
    }

    public async Task<ApiResult> DeleteSuggestionAsync(Guid id)
    {
        var response = await http.DeleteAsync($"/api/suggestions/{id}");
        return await response.Content.ReadFromJsonAsync<ApiResult>()
            ?? ApiResult.Fail("Failed to delete suggestion");
    }

    public async Task<ApiResult> ReorderSuggestionAsync(ReorderSuggestionRequest request)
    {
        var response = await http.PostAsJsonAsync("/api/suggestions/reorder", request);
        return await response.Content.ReadFromJsonAsync<ApiResult>()
            ?? ApiResult.Fail("Failed to reorder suggestion");
    }
}
