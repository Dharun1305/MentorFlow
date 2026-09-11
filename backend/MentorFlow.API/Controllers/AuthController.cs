using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MentorFlow.API.DTOs;
using MentorFlow.API.Interfaces;

namespace MentorFlow.API.Controllers;

[ApiController, Route("api/auth")]
public sealed class AuthController(IAuthService authService) : ControllerBase
{
    [AllowAnonymous, HttpPost("login")]
    public Task<LoginResponse> Login(LoginRequest request, CancellationToken cancellationToken) => authService.LoginAsync(request, cancellationToken);

    [Authorize, HttpGet("me")]
    public Task<UserResponse> Me(CancellationToken cancellationToken) => authService.GetCurrentUserAsync(cancellationToken);
}
