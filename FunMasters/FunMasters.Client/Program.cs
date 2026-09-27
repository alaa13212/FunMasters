using FunMasters.Client.Authentication;
using FunMasters.Client.Services;
using FunMasters.Shared.Services;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;

var builder = WebAssemblyHostBuilder.CreateDefault(args);

builder.Services.AddAuthorizationCore();
builder.Services.AddCascadingAuthenticationState();
builder.Services.AddSingleton<AuthenticationStateProvider, PersistentAuthenticationStateProvider>();


builder.Services.AddScoped(_ => new HttpClient
{
    BaseAddress = new Uri(builder.HostEnvironment.BaseAddress)
});

// Register client services (HttpClient implementations of shared interfaces)
builder.Services.AddScoped<ISuggestionApiService, SuggestionApiService>();
builder.Services.AddScoped<IRatingApiService, RatingApiService>();
builder.Services.AddScoped<IAdminApiService, AdminApiService>();
builder.Services.AddScoped<IAccountApiService, AccountApiService>();
builder.Services.AddScoped<IIgdbApiService, IgdbApiService>();
builder.Services.AddScoped<IHltbApiService, HltbApiService>();
builder.Services.AddScoped<ISteamApiService, SteamApiService>();
builder.Services.AddScoped<IGemApiService, GemApiService>();
builder.Services.AddScoped<IMemberApiService, MemberApiService>();
builder.Services.AddScoped<IPublicDataApiService, PublicDataApiService>();

await builder.Build().RunAsync();
