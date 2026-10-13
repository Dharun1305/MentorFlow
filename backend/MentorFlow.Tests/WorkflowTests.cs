using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using MentorFlow.API.Data;
using MentorFlow.API.DTOs;
using MentorFlow.API.Entities;
using MentorFlow.API.Interfaces;
using MentorFlow.API.Services;
using Xunit;

namespace MentorFlow.Tests;
public sealed class WorkflowTests
{
    private static readonly CancellationToken Ct = CancellationToken.None;
    [Fact]
    public async Task StudentMentorResponseDoesNotRevealOtherStudents()
    {
        await using var db = await CreateDb();
        var service = new MentorService(db, new PasswordHasher<User>(), new Caller(3, "Student"));
        var page = await service.GetAllAsync(new ProfileListRequest(), Ct);
        var mentor = Assert.Single(page.Items);
        Assert.Equal(2, mentor.MentorId);
        Assert.Equal(3, Assert.Single(mentor.Assignments).StudentId);
        Assert.Equal(3, Assert.Single((await service.GetByIdAsync(2, Ct)).Assignments).StudentId);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task MentorDeactivationEndsAssignmentsForBothUpdateAndDelete(bool update)
    {
        await using var db = await CreateDb();
        var service = new MentorService(db, new PasswordHasher<User>(), new Caller(1, "Admin"));
        if (update)
            await service.UpdateAsync(2, new UpdateMentorRequest { FirstName = "Mentor", LastName = "Two", Email = "mentor@test.local", EmployeeCode = "M2", IsActive = false }, Ct);
        else
            await service.DeleteAsync(2, Ct);
        Assert.False((await db.Users.FindAsync(2))!.IsActive);
        Assert.All(await db.MentorStudents.ToListAsync(), assignment =>
        {
            Assert.False(assignment.IsActive);
            Assert.NotNull(assignment.UnassignedAt);
        });
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task StudentDeactivationEndsOnlyTheirAssignment(bool update)
    {
        await using var db = await CreateDb();
        var service = new StudentService(db, new PasswordHasher<User>(), new Caller(1, "Admin"));
        if (update)
            await service.UpdateAsync(3, new UpdateStudentRequest { FirstName = "Student", LastName = "Three", Email = "student3@test.local", StudentCode = "S3", IsActive = false }, Ct);
        else
            await service.DeleteAsync(3, Ct);
        Assert.False((await db.Users.FindAsync(3))!.IsActive);
        Assert.False((await db.MentorStudents.FindAsync(1))!.IsActive);
        Assert.True((await db.MentorStudents.FindAsync(2))!.IsActive);
    }

    [Fact]
    public async Task ReassignmentPreservesOriginalPeriodAndCreatesAnother()
    {
        await using var db = await CreateDb();
        var service = new AssignmentService(db, new Caller(1, "Admin"));
        var originalDate = (await db.MentorStudents.FindAsync(1))!.AssignedAt;
        await service.UpdateAsync(1, new UpdateAssignmentRequest { IsActive = false }, Ct);
        var next = await service.CreateAsync(new CreateAssignmentRequest { MentorId = 2, StudentId = 3 }, Ct);
        Assert.NotEqual(1, next.MentorStudentId);
        Assert.Equal(originalDate, (await db.MentorStudents.FindAsync(1))!.AssignedAt);
        Assert.False((await db.MentorStudents.FindAsync(1))!.IsActive);
        Assert.Equal(2, await db.MentorStudents.CountAsync(item => item.StudentId == 3));
        var error = await Assert.ThrowsAsync<ApiException>(() => service.UpdateAsync(1, new UpdateAssignmentRequest { IsActive = true }, Ct));
        Assert.Equal(409, error.StatusCode);
    }

    [Fact]
    public async Task DuplicateActiveAssignmentIsRejected()
    {
        await using var db = await CreateDb();
        var service = new AssignmentService(db, new Caller(1, "Admin"));
        var error = await Assert.ThrowsAsync<ApiException>(() => service.CreateAsync(new CreateAssignmentRequest { MentorId = 2, StudentId = 3 }, Ct));
        Assert.Equal(409, error.StatusCode);
    }

    [Fact]
    public async Task FormerMentorCanReadHistoryButCannotEditIt()
    {
        await using var db = await CreateDb();
        (await db.MentorStudents.FindAsync(1))!.IsActive = false;
        await db.SaveChangesAsync();
        var caller = new Caller(2, "Mentor");
        var meetings = new MeetingService(db, caller);
        var goals = new GoalService(db, caller);
        Assert.Equal(1, (await meetings.GetByIdAsync(1, Ct)).MeetingId);
        Assert.Equal(1, (await goals.GetByIdAsync(1, Ct)).GoalId);
        var meetingError = await Assert.ThrowsAsync<ApiException>(() => meetings.UpdateAsync(1, new UpdateMeetingRequest { MeetingAt = DateTimeOffset.UtcNow, Topic = "Changed", RowVersion = new byte[8] }, Ct));
        var goalError = await Assert.ThrowsAsync<ApiException>(() => goals.UpdateAsync(1, new UpdateGoalRequest { Title = "Changed", Status = "InProgress", Priority = "High", RowVersion = new byte[8] }, Ct));
        Assert.Equal(409, meetingError.StatusCode);
        Assert.Equal(409, goalError.StatusCode);
    }

    [Fact]
    public async Task UnrelatedMentorCannotReadMeetingOrGoal()
    {
        await using var db = await CreateDb();
        var caller = new Caller(99, "Mentor");
        Assert.Equal(403, (await Assert.ThrowsAsync<ApiException>(() => new MeetingService(db, caller).GetByIdAsync(1, Ct))).StatusCode);
        Assert.Equal(403, (await Assert.ThrowsAsync<ApiException>(() => new GoalService(db, caller).GetByIdAsync(1, Ct))).StatusCode);
    }

    [Fact]
    public async Task MeetingOffsetsAreStoredAndReturnedAsUtc()
    {
        await using var db = await CreateDb();
        var service = new MeetingService(db, new Caller(2, "Mentor"));
        var result = await service.CreateAsync(new CreateMeetingRequest { MentorStudentId = 1, Topic = "Timezone", MeetingAt = new DateTimeOffset(2026, 10, 13, 18, 0, 0, TimeSpan.FromHours(5.5)) }, Ct);
        Assert.Equal(new DateTime(2026, 10, 13, 12, 30, 0, DateTimeKind.Utc), result.MeetingAt);
        Assert.Equal(DateTimeKind.Utc, result.MeetingAt.Kind);
    }

    [Fact]
    public async Task FiltersAndPagingCannotExpandStudentAccess()
    {
        await using var db = await CreateDb();
        var service = new AssignmentService(db, new Caller(3, "Student"));
        var page = await service.GetAllAsync(new AssignmentListRequest { StudentId = 4 }, Ct);
        Assert.Empty(page.Items);
        Assert.Equal(0, page.TotalCount);
        var admin = new AssignmentService(db, new Caller(1, "Admin"));
        var first = await admin.GetAllAsync(new AssignmentListRequest { PageSize = 1 }, Ct);
        var second = await admin.GetAllAsync(new AssignmentListRequest { PageSize = 1, Page = 2 }, Ct);
        Assert.Equal(2, first.TotalCount);
        Assert.NotEqual(Assert.Single(first.Items).MentorStudentId, Assert.Single(second.Items).MentorStudentId);
    }

    [Fact]
    public async Task DeactivatedUserCannotLogin()
    {
        await using var db = await CreateDb();
        var user = (await db.Users.FindAsync(2))!;
        var hasher = new PasswordHasher<User>();
        user.PasswordHash = hasher.HashPassword(user, "TestPassword123!");
        user.IsActive = false;
        await db.SaveChangesAsync();
        var service = new AuthService(db, hasher, new Caller(2, "Mentor"), new ConfigurationBuilder().Build());
        Assert.Equal(401, (await Assert.ThrowsAsync<ApiException>(() => service.LoginAsync(new LoginRequest { Email = user.Email, Password = "TestPassword123!" }, Ct))).StatusCode);
    }

    [Fact]
    public void SqlModelEnforcesSingleActiveAssignmentAndVersionedEdits()
    {
        using var db = new MentorFlowDbContext(new DbContextOptionsBuilder<MentorFlowDbContext>().UseSqlServer("Server=unused;Database=unused").Options);
        var assignment = db.Model.FindEntityType(typeof(MentorStudent))!;
        Assert.Contains(assignment.GetIndexes(), index => index.IsUnique && index.GetFilter() == "[IsActive] = 1" && index.Properties.Single().Name == "StudentId");
        Assert.DoesNotContain(assignment.GetIndexes(), index => index.IsUnique && index.Properties.Count == 2);
        foreach (var type in new[]
        {
            typeof(Meeting),
            typeof(Goal)
        }

        )
        {
            var version = db.Model.FindEntityType(type)!.FindProperty("RowVersion")!;
            Assert.True(version.IsConcurrencyToken);
        }
    }

    [Fact]
    public async Task StaleGoalEditIsRejectedWithoutOverwritingNewerData()
    {
        await using var db = await CreateDb();
        var goal = (await db.Goals.FindAsync(1))!;
        goal.RowVersion = [0, 0, 0, 0, 0, 0, 0, 1];
        goal.Title = "Newer title";
        await db.SaveChangesAsync();
        db.ChangeTracker.Clear();
        var service = new GoalService(db, new Caller(2, "Mentor"));
        await Assert.ThrowsAsync<DbUpdateConcurrencyException>(() => service.UpdateAsync(1, new UpdateGoalRequest { Title = "Stale title", Status = "InProgress", Priority = "High", RowVersion = new byte[8] }, Ct));
        db.ChangeTracker.Clear();
        Assert.Equal("Newer title", (await db.Goals.FindAsync(1))!.Title);
    }

    private static async Task<MentorFlowDbContext> CreateDb()
    {
        var db = new MentorFlowDbContext(new DbContextOptionsBuilder<MentorFlowDbContext>().UseInMemoryDatabase(Guid.NewGuid().ToString()).Options);
        var now = DateTime.UtcNow.AddDays(-1);
        var adminRole = new Role
        {
            RoleId = 1,
            Name = "Admin",
            IsActive = true
        };
        var mentorRole = new Role
        {
            RoleId = 2,
            Name = "Mentor",
            IsActive = true
        };
        var studentRole = new Role
        {
            RoleId = 3,
            Name = "Student",
            IsActive = true
        };
        User Account(int id, Role role, string email) => new()
        {
            UserId = id,
            Role = role,
            FirstName = role.Name,
            LastName = id.ToString(),
            Email = email,
            PasswordHash = "unused",
            IsActive = true
        };
        db.Users.Add(Account(1, adminRole, "admin@test.local"));
        var mentor = new Mentor
        {
            MentorId = 2,
            User = Account(2, mentorRole, "mentor@test.local"),
            EmployeeCode = "M2",
            IsActive = true
        };
        var student3 = new Student
        {
            StudentId = 3,
            User = Account(3, studentRole, "student3@test.local"),
            StudentCode = "S3",
            IsActive = true
        };
        var student4 = new Student
        {
            StudentId = 4,
            User = Account(4, studentRole, "student4@test.local"),
            StudentCode = "S4",
            IsActive = true
        };
        var assignment = new MentorStudent
        {
            MentorStudentId = 1,
            Mentor = mentor,
            Student = student3,
            AssignedAt = now,
            IsActive = true
        };
        db.MentorStudents.AddRange(assignment, new MentorStudent { MentorStudentId = 2, Mentor = mentor, Student = student4, AssignedAt = now, IsActive = true });
        db.Meetings.Add(new Meeting { MeetingId = 1, MentorStudent = assignment, MeetingAt = now, Topic = "Initial", IsActive = true, RowVersion = new byte[8] });
        db.Goals.Add(new Goal { GoalId = 1, MentorStudent = assignment, Title = "Initial", Status = "NotStarted", Priority = "Medium", IsActive = true, RowVersion = new byte[8] });
        await db.SaveChangesAsync();
        db.ChangeTracker.Clear();
        return db;
    }

    private sealed record Caller(int UserId, string Role) : ICurrentUserService
    {
        public bool IsAuthenticated => true;
    }
}
