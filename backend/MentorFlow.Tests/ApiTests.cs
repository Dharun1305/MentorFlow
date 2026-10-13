using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using MentorFlow.API.Data;
using MentorFlow.API.DTOs;
using MentorFlow.API.Entities;
using Xunit;

namespace MentorFlow.Tests;
public sealed class ApiTests
{
    [Fact]
    public async Task ExistingJwtIsRejectedAfterAccountDeactivation()
    {
        using var factory = new ApiFactory();
        using var client = factory.CreateClient(new WebApplicationFactoryClientOptions { BaseAddress = new Uri("https://localhost") });
        int userId;
        await using (var scope = factory.Services.CreateAsyncScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<MentorFlowDbContext>();
            var user = new User
            {
                Role = new Role
                {
                    Name = "Admin",
                    IsActive = true
                },
                FirstName = "Test",
                LastName = "Admin",
                Email = "admin@test.local",
                IsActive = true
            };
            user.PasswordHash = new PasswordHasher<User>().HashPassword(user, "TestPassword123!");
            db.Users.Add(user);
            await db.SaveChangesAsync();
            userId = user.UserId;
        }

        var response = await client.PostAsJsonAsync("/api/auth/login", new { email = "admin@test.local", password = "TestPassword123!" });
        response.EnsureSuccessStatusCode();
        var login = (await response.Content.ReadFromJsonAsync<LoginResponse>())!;
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", login.Token);
        Assert.Equal(HttpStatusCode.OK, (await client.GetAsync("/api/auth/me")).StatusCode);
        await using (var scope = factory.Services.CreateAsyncScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<MentorFlowDbContext>();
            (await db.Users.FindAsync(userId))!.IsActive = false;
            await db.SaveChangesAsync();
        }

        Assert.Equal(HttpStatusCode.Unauthorized, (await client.GetAsync("/api/auth/me")).StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, (await client.GetAsync("/api/students")).StatusCode);
    }

    [Fact]
    public async Task InvalidLoginRequestUsesCamelCaseErrors()
    {
        using var factory = new ApiFactory();
        using var client = factory.CreateClient();
        var response = await client.PostAsJsonAsync("/api/auth/login", new { email = "invalid", password = "short" });
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        using var json = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        Assert.True(json.RootElement.TryGetProperty("message", out _));
        Assert.True(json.RootElement.TryGetProperty("errors", out _));
    }

    private sealed class ApiFactory : WebApplicationFactory<Program>
    {
        private readonly string database = Guid.NewGuid().ToString();
        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            builder.UseEnvironment("Testing");
            builder.UseSetting("Jwt:Key", "test-only-signing-key-at-least-thirty-two-bytes");
            builder.ConfigureServices(services =>
            {
                services.RemoveAll<DbContextOptions<MentorFlowDbContext>>();
                services.RemoveAll<Microsoft.EntityFrameworkCore.Infrastructure.IDbContextOptionsConfiguration<MentorFlowDbContext>>();
                services.AddDbContext<MentorFlowDbContext>(options => options.UseInMemoryDatabase(database));
            });
        }
    }
}
