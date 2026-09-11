using System.Security.Claims;
using MentorFlow.API.Interfaces;

namespace MentorFlow.API.Services;

public sealed class CurrentUserService(IHttpContextAccessor httpContextAccessor) : ICurrentUserService
{
    private ClaimsPrincipal User => httpContextAccessor.HttpContext?.User ?? new ClaimsPrincipal();
    public bool IsAuthenticated => User.Identity?.IsAuthenticated == true;
    public int UserId => int.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var value) ? value : 0;
    public string Role => User.FindFirstValue(ClaimTypes.Role) ?? string.Empty;
}
