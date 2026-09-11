using MentorFlow.API.DTOs;

namespace MentorFlow.API.Interfaces;

public interface IAuthService
{
    Task<LoginResponse> LoginAsync(LoginRequest request, CancellationToken cancellationToken);
    Task<UserResponse> GetCurrentUserAsync(CancellationToken cancellationToken);
}
