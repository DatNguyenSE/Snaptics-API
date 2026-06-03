using System.Security.Claims;

namespace API.Extensions; 
public static class ClaimsPrincipalExtensions
{
    public static string GetUserId(this ClaimsPrincipal user)
    {
        return user.FindFirstValue(ClaimTypes.NameIdentifier) 
               ?? throw new UnauthorizedAccessException("User ID not found in token.");
    }
}