using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Components.Server;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Options;
using System.Security.Claims;
using FunMasters.Data;
using FunMasters.Shared.DTOs;

namespace FunMasters.Authentication;

public class PersistingRevalidatingAuthenticationStateProvider : ServerAuthenticationStateProvider, IDisposable
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly PersistentComponentState _state;
    private readonly IdentityOptions _options;
    private readonly PersistingComponentStateSubscription _subscription;

    private Task<AuthenticationState>? _authenticationStateTask;
    private readonly Timer? _revalidationTimer;

    public PersistingRevalidatingAuthenticationStateProvider(
        IServiceScopeFactory scopeFactory,
        PersistentComponentState persistentComponentState,
        IOptions<IdentityOptions> optionsAccessor)
    {
        _scopeFactory = scopeFactory;
        _state = persistentComponentState;
        _options = optionsAccessor.Value;

        AuthenticationStateChanged += OnAuthenticationStateChanged;
        _subscription = _state.RegisterOnPersisting(OnPersistingAsync, RenderMode.InteractiveWebAssembly);

        // Start revalidation timer (every 30 minutes)
        _revalidationTimer = new Timer(RevalidateAsync, null, TimeSpan.FromMinutes(30), TimeSpan.FromMinutes(30));
    }

    private void OnAuthenticationStateChanged(Task<AuthenticationState> task)
    {
        _authenticationStateTask = task;
    }

    private async Task OnPersistingAsync()
    {
        if (_authenticationStateTask is null)
        {
            return;
        }

        var authenticationState = await _authenticationStateTask;
        var principal = authenticationState.User;

        if (principal.Identity?.IsAuthenticated == true)
        {
            List<string> roles = principal.FindAll(_options.ClaimsIdentity.RoleClaimType).Select(c => c.Value).ToList();
            var userId = principal.FindFirst(_options.ClaimsIdentity.UserIdClaimType)?.Value;
            var email = principal.FindFirst(_options.ClaimsIdentity.EmailClaimType)?.Value;
            var name = principal.FindFirst(ClaimTypes.Name)?.Value;
            var avatarTimestamp = principal.FindFirst("avatar_timestamp")?.Value;

            if (userId != null)
            {
                _state.PersistAsJson(nameof(LoggedInUserInfo), new LoggedInUserInfo
                {
                    UserId = userId,
                    Email = email ?? string.Empty,
                    DisplayName = name ?? string.Empty,
                    AvatarTimestamp = avatarTimestamp ?? string.Empty,
                    Roles = roles,
                });
            }
        }
    }

    private async void RevalidateAsync(object? state)
    {
        try
        {
            var authState = await GetAuthenticationStateAsync();
            var isValid = await ValidateAuthenticationStateAsync(authState);

            if (!isValid)
            {
                NotifyAuthenticationStateChanged(Task.FromResult(new AuthenticationState(new ClaimsPrincipal())));
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine(ex);
        }
    }

    private async Task<bool> ValidateAuthenticationStateAsync(AuthenticationState authenticationState)
    {
        if (authenticationState.User.Identity?.IsAuthenticated != true)
        {
            return false;
        }

        await using var scope = _scopeFactory.CreateAsyncScope();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
        return await ValidateSecurityStampAsync(userManager, authenticationState.User);
    }

    private async Task<bool> ValidateSecurityStampAsync(UserManager<ApplicationUser> userManager, ClaimsPrincipal principal)
    {
        var user = await userManager.GetUserAsync(principal);
        if (user is null)
        {
            return false;
        }

        if (!userManager.SupportsUserSecurityStamp)
        {
            return true;
        }

        var principalStamp = principal.FindFirstValue(_options.ClaimsIdentity.SecurityStampClaimType);
        var userStamp = await userManager.GetSecurityStampAsync(user);
        return principalStamp == userStamp;
    }

    public void Dispose()
    {
        _subscription.Dispose();
        _revalidationTimer?.Dispose();
        AuthenticationStateChanged -= OnAuthenticationStateChanged;
    }
}