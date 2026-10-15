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
        Assert.Equal(HttpStatusCode.BadRequest, (await client.PatchAsJsonAsync("/api/students/999/status", new { })).StatusCode);
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

    [Theory]
    [InlineData("Mentor")]
    [InlineData("Student")]
    public async Task NonAdminCannotChangeStatusOrTransferStudents(string role)
    {
        using var factory = new ApiFactory();
        using var client = factory.CreateClient(new WebApplicationFactoryClientOptions { BaseAddress = new Uri("https://localhost") });
        await using (var scope = factory.Services.CreateAsyncScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<MentorFlowDbContext>();
            var user = new User { Role = new Role { Name = role, IsActive = true }, FirstName = "Test", LastName = role, Email = "test@example.local", IsActive = true };
            user.PasswordHash = new PasswordHasher<User>().HashPassword(user, "TestPassword123!");
            if (role == "Mentor") db.Mentors.Add(new Mentor { User = user, EmployeeCode = "M1", IsActive = true });
            else db.Students.Add(new Student { User = user, StudentCode = "S1", IsActive = true });
            await db.SaveChangesAsync();
        }
        var response = await client.PostAsJsonAsync("/api/auth/login", new { email = "test@example.local", password = "TestPassword123!" });
        response.EnsureSuccessStatusCode();
        var login = (await response.Content.ReadFromJsonAsync<LoginResponse>())!;
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", login.Token);
        Assert.Equal(HttpStatusCode.Forbidden, (await client.PatchAsJsonAsync("/api/students/1/status", new { isActive = false })).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, (await client.PatchAsJsonAsync("/api/mentors/1/status", new { isActive = false })).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, (await client.PostAsJsonAsync("/api/assignments/1/reassign", new { mentorId = 2 })).StatusCode);
    }

    [Fact]
    public async Task AdminCanCreateAccountsTransferAndPreserveAssignmentHistory()
    {
        using var factory = new ApiFactory();
        using var client = factory.CreateClient(new WebApplicationFactoryClientOptions { BaseAddress = new Uri("https://localhost") });
        await using (var scope = factory.Services.CreateAsyncScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<MentorFlowDbContext>();
            var adminRole = new Role { Name = "Admin", IsActive = true };
            db.Roles.AddRange(adminRole, new Role { Name = "Mentor", IsActive = true }, new Role { Name = "Student", IsActive = true });
            var user = new User { Role = adminRole, FirstName = "Dev", LastName = "Admin", Email = "admin@example.test", IsActive = true };
            user.PasswordHash = new PasswordHasher<User>().HashPassword(user, "TestPassword123!");
            db.Users.Add(user);
            await db.SaveChangesAsync();
        }
        var loginResponse = await client.PostAsJsonAsync("/api/auth/login", new { email = "admin@example.test", password = "TestPassword123!" });
        loginResponse.EnsureSuccessStatusCode();
        var login = (await loginResponse.Content.ReadFromJsonAsync<LoginResponse>())!;
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", login.Token);
        var firstResponse = await client.PostAsJsonAsync("/api/mentors", new { firstName = "First", lastName = "Mentor", email = "first@example.test", employeeCode = "M1", password = "TestPassword123!" });
        firstResponse.EnsureSuccessStatusCode();
        var first = (await firstResponse.Content.ReadFromJsonAsync<MentorResponse>())!;
        var secondResponse = await client.PostAsJsonAsync("/api/mentors", new { firstName = "Second", lastName = "Mentor", email = "second@example.test", employeeCode = "M2", password = "TestPassword123!" });
        secondResponse.EnsureSuccessStatusCode();
        var second = (await secondResponse.Content.ReadFromJsonAsync<MentorResponse>())!;
        var studentResponse = await client.PostAsJsonAsync("/api/students", new { firstName = "Test", lastName = "Student", email = "student@example.test", studentCode = "S1", password = "TestPassword123!" });
        studentResponse.EnsureSuccessStatusCode();
        var student = (await studentResponse.Content.ReadFromJsonAsync<StudentResponse>())!;
        Assert.NotEqual(student.UserId, student.StudentId);
        var createdResponse = await client.PostAsJsonAsync("/api/assignments", new { studentId = student.StudentId, mentorId = first.MentorId });
        createdResponse.EnsureSuccessStatusCode();
        var created = (await createdResponse.Content.ReadFromJsonAsync<AssignmentSummary>())!;
        var transferResponse = await client.PostAsJsonAsync($"/api/assignments/{created.MentorStudentId}/reassign", new { mentorId = second.MentorId });
        transferResponse.EnsureSuccessStatusCode();
        var transferred = (await transferResponse.Content.ReadFromJsonAsync<AssignmentSummary>())!;
        Assert.NotEqual(created.MentorStudentId, transferred.MentorStudentId);
        var history = (await client.GetFromJsonAsync<PagedResponse<AssignmentSummary>>($"/api/assignments?studentId={student.StudentId}"))!;
        Assert.Equal(2, history.TotalCount);
        Assert.Single(history.Items, item => item.IsActive);
        Assert.False(history.Items.Single(item => item.MentorStudentId == created.MentorStudentId).IsActive);
        (await client.PatchAsJsonAsync($"/api/students/{student.StudentId}/status", new { isActive = false })).EnsureSuccessStatusCode();
        var editedResponse = await client.PutAsJsonAsync($"/api/students/{student.StudentId}", new { firstName = "Edited", lastName = "Student", email = student.Email, studentCode = student.StudentCode });
        editedResponse.EnsureSuccessStatusCode();
        Assert.False((await editedResponse.Content.ReadFromJsonAsync<StudentResponse>())!.IsActive);
        var activatedResponse = await client.PatchAsJsonAsync($"/api/students/{student.StudentId}/status", new { isActive = true });
        activatedResponse.EnsureSuccessStatusCode();
        Assert.Null((await activatedResponse.Content.ReadFromJsonAsync<StudentResponse>())!.ActiveAssignment);
        var nextResponse = await client.PostAsJsonAsync("/api/assignments", new { studentId = student.StudentId, mentorId = second.MentorId });
        nextResponse.EnsureSuccessStatusCode();
        var next = (await nextResponse.Content.ReadFromJsonAsync<AssignmentSummary>())!;
        (await client.PutAsJsonAsync($"/api/assignments/{next.MentorStudentId}", new { isActive = false })).EnsureSuccessStatusCode();
        var finalHistory = (await client.GetFromJsonAsync<PagedResponse<AssignmentSummary>>($"/api/assignments?studentId={student.StudentId}"))!;
        Assert.Equal(3, finalHistory.TotalCount);
        Assert.All(finalHistory.Items, item => Assert.False(item.IsActive));
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
