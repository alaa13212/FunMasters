using System.Security.Claims;
using FunMasters.Shared.DTOs;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Authorization;

namespace FunMasters.Client.Authentication;

public class PersistentAuthenticationStateProvider : AuthenticationStateProvider
{
    private static readonly Task<AuthenticationState> UnauthenticatedTask =
        Task.FromResult(new AuthenticationState(new ClaimsPrincipal(new ClaimsIdentity())));

    private readonly Task<AuthenticationState> _authenticationStateTask = UnauthenticatedTask;

    public PersistentAuthenticationStateProvider(PersistentComponentState state)
    {
        if (!state.TryTakeFromJson<LoggedInUserInfo>(nameof(LoggedInUserInfo), out var userInfo) || userInfo is null)
        {
            return;
        }

        var claims = new List<Claim>
        {
            new Claim(ClaimTypes.NameIdentifier, userInfo.UserId),
            new Claim(ClaimTypes.Email, userInfo.Email),
            new Claim(ClaimTypes.Name, userInfo.DisplayName),
            new Claim("avatar_timestamp", userInfo.AvatarTimestamp),
        };
        
        claims.AddRange(userInfo.Roles.Select(role => new Claim(ClaimTypes.Role, role)));

        _authenticationStateTask = Task.FromResult(new AuthenticationState(new ClaimsPrincipal(new ClaimsIdentity(claims, authenticationType: nameof(PersistentAuthenticationStateProvider)))));
    }

    public override Task<AuthenticationState> GetAuthenticationStateAsync()
    {
        return _authenticationStateTask;
    }
}