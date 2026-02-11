using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;

namespace FunMasters.Services;

/// <summary>
/// Adds avatar_timestamp claim to user's identity for cache busting in WASM
/// </summary>
public class AvatarClaimsTransformation(AvatarStorage avatarStorage) : IClaimsTransformation
{
    public Task<ClaimsPrincipal> TransformAsync(ClaimsPrincipal principal)
    {
        if (principal.Identity?.IsAuthenticated != true)
            return Task.FromResult(principal);

        var userIdClaim = principal.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (userIdClaim == null || !Guid.TryParse(userIdClaim, out var userId))
            return Task.FromResult(principal);

        try
        {
            // Get the current timestamp from AvatarStorage
            string newTimestamp = avatarStorage.GetFileTimestamp(userId);
            Claim? existingClaim = principal.FindFirst("avatar_timestamp");

            // Only update if timestamp has changed or doesn't exist
            if (existingClaim == null || existingClaim.Value != newTimestamp)
            {
                var identity = (ClaimsIdentity)principal.Identity;

                // Remove old claim if exists
                if (existingClaim != null)
                {
                    identity.RemoveClaim(existingClaim);
                }

                // Add new timestamp claim
                identity.AddClaim(new Claim("avatar_timestamp", newTimestamp));
            }
            
        }
        catch
        {
            // If there's any error getting the timestamp, just skip it
            // User will still be authenticated, just won't have avatar cache busting
        }

        return Task.FromResult(principal);
    }
}
