using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using MentorFlow.API.Data;
using MentorFlow.API.DTOs;
using MentorFlow.API.Entities;
using MentorFlow.API.Interfaces;

namespace MentorFlow.API.Services;
public sealed class AuthService(MentorFlowDbContext db, IPasswordHasher<User> passwordHasher, ICurrentUserService currentUser, IConfiguration configuration) : IAuthService
{
    public async Task<LoginResponse> LoginAsync(LoginRequest request, CancellationToken cancellationToken)
    {
        var email = request.Email.Trim().ToLowerInvariant();
        var user = await db.Users
            .Include(user => user.Role)
            .SingleOrDefaultAsync(user => user.Email == email && user.IsActive && user.Role.IsActive && (user.Role.Name == "Admin" || (user.Role.Name == "Mentor" && user.Mentor != null && user.Mentor.IsActive) || (user.Role.Name == "Student" && user.Student != null && user.Student.IsActive)), cancellationToken);
        if (user is null || passwordHasher.VerifyHashedPassword(user, user.PasswordHash, request.Password) == PasswordVerificationResult.Failed)
            throw new ApiException(StatusCodes.Status401Unauthorized, "Invalid email or password.");
        var expiresAt = DateTime.UtcNow.AddMinutes(configuration.GetValue("Jwt:ExpiresMinutes", 120));
        var claims = new[]
        {
            new Claim(JwtRegisteredClaimNames.Sub, user.UserId.ToString()),
            new Claim(ClaimTypes.NameIdentifier, user.UserId.ToString()),
            new Claim(ClaimTypes.Email, user.Email),
            new Claim(ClaimTypes.Role, user.Role.Name)
        };
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(configuration["Jwt:Key"]!));
        var token = new JwtSecurityToken(issuer: configuration["Jwt:Issuer"], audience: configuration["Jwt:Audience"], claims: claims, expires: expiresAt, signingCredentials: new SigningCredentials(key, SecurityAlgorithms.HmacSha256));
        return new LoginResponse(new JwtSecurityTokenHandler().WriteToken(token), expiresAt, ToResponse(user));
    }

    public async Task<UserResponse> GetCurrentUserAsync(CancellationToken cancellationToken)
    {
        var user = await db.Users
            .Include(user => user.Role)
            .SingleOrDefaultAsync(user => user.UserId == currentUser.UserId && user.IsActive, cancellationToken) ?? throw new ApiException(StatusCodes.Status404NotFound, "User not found.");
        return ToResponse(user);
    }

    internal static UserResponse ToResponse(User user) => new(user.UserId, user.FirstName, user.LastName, user.Email, user.Role.Name);
}
