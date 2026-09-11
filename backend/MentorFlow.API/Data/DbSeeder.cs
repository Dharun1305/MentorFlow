using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using MentorFlow.API.Entities;

namespace MentorFlow.API.Data;

public static class DbSeeder
{
    private const string DevelopmentPassword = "MentorFlow123!";

    public static async Task SeedAsync(MentorFlowDbContext db, IPasswordHasher<User> passwordHasher)
    {
        var now = DateTime.UtcNow;
        var roles = new Dictionary<string, Role>();
        foreach (var name in new[] { "Admin", "Mentor", "Student" })
        {
            var role = await db.Roles.SingleOrDefaultAsync(item => item.Name == name);
            if (role is null)
            {
                role = new Role { Name = name, Description = $"MentorFlow {name} role", IsActive = true, CreatedAt = now, UpdatedAt = now };
                db.Roles.Add(role);
            }
            roles[name] = role;
        }
        await db.SaveChangesAsync();

        var admin = await GetOrCreateUserAsync(db, passwordHasher, roles["Admin"], "Dev", "Admin", "admin.dev@mentorflow.local", "DEV-ADMIN-PASSWORD-HASH", now);
        var mentorUser = await GetOrCreateUserAsync(db, passwordHasher, roles["Mentor"], "Dev", "Mentor", "mentor.dev@mentorflow.local", "DEV-MENTOR-PASSWORD-HASH", now);
        var studentUser = await GetOrCreateUserAsync(db, passwordHasher, roles["Student"], "Dev", "Student", "student.dev@mentorflow.local", "DEV-STUDENT-PASSWORD-HASH", now);

        var mentor = await db.Mentors.SingleOrDefaultAsync(item => item.EmployeeCode == "DEV-MENTOR-001");
        if (mentor is null) { mentor = new Mentor { UserId = mentorUser.UserId, EmployeeCode = "DEV-MENTOR-001", Specialization = "General Student Development", IsActive = true, CreatedAt = now, UpdatedAt = now }; db.Mentors.Add(mentor); }
        var student = await db.Students.SingleOrDefaultAsync(item => item.StudentCode == "DEV-STUDENT-001");
        if (student is null) { student = new Student { UserId = studentUser.UserId, StudentCode = "DEV-STUDENT-001", IsActive = true, CreatedAt = now, UpdatedAt = now }; db.Students.Add(student); }
        await db.SaveChangesAsync();

        var assignment = await db.MentorStudents.SingleOrDefaultAsync(item => item.MentorId == mentor.MentorId && item.StudentId == student.StudentId);
        if (assignment is null) { assignment = new MentorStudent { MentorId = mentor.MentorId, StudentId = student.StudentId, AssignedAt = now, CreatedAt = now, UpdatedAt = now, IsActive = true }; db.MentorStudents.Add(assignment); await db.SaveChangesAsync(); }

        if (!await db.Meetings.AnyAsync(item => item.MentorStudentId == assignment.MentorStudentId && item.Topic == "DEV - Initial mentorship meeting"))
            db.Meetings.Add(new Meeting { MentorStudentId = assignment.MentorStudentId, MeetingAt = new DateTime(2026, 1, 15, 10, 0, 0, DateTimeKind.Utc), DurationMinutes = 60, Topic = "DEV - Initial mentorship meeting", Notes = "Development seed meeting for local testing.", Feedback = "DEV - Student is engaged and ready to define initial goals.", CreatedAt = now, UpdatedAt = now, IsActive = true });
        if (!await db.Goals.AnyAsync(item => item.MentorStudentId == assignment.MentorStudentId && item.Title == "DEV - Complete onboarding plan"))
            db.Goals.Add(new Goal { MentorStudentId = assignment.MentorStudentId, Title = "DEV - Complete onboarding plan", Description = "Finish the initial MentorFlow student onboarding checklist.", TargetDate = new DateTime(2026, 2, 15), Status = "InProgress", Priority = "High", CreatedAt = now, UpdatedAt = now, IsActive = true });
        if (!await db.Goals.AnyAsync(item => item.MentorStudentId == assignment.MentorStudentId && item.Title == "DEV - Prepare monthly progress reflection"))
            db.Goals.Add(new Goal { MentorStudentId = assignment.MentorStudentId, Title = "DEV - Prepare monthly progress reflection", Description = "Write a short reflection before the next mentoring meeting.", TargetDate = new DateTime(2026, 2, 28), Status = "NotStarted", Priority = "Medium", CreatedAt = now, UpdatedAt = now, IsActive = true });
        await db.SaveChangesAsync();
    }

    private static async Task<User> GetOrCreateUserAsync(MentorFlowDbContext db, IPasswordHasher<User> hasher, Role role, string firstName, string lastName, string email, string placeholder, DateTime now)
    {
        var user = await db.Users.SingleOrDefaultAsync(item => item.Email == email);
        if (user is null)
        {
            user = new User { RoleId = role.RoleId, FirstName = firstName, LastName = lastName, Email = email, IsActive = true, CreatedAt = now, UpdatedAt = now };
            user.PasswordHash = hasher.HashPassword(user, DevelopmentPassword); db.Users.Add(user); await db.SaveChangesAsync();
        }
        else if (user.PasswordHash == placeholder || user.PasswordHash == "DEV_ONLY_REPLACE_WITH_A_REAL_PASSWORD_HASH")
        {
            user.PasswordHash = hasher.HashPassword(user, DevelopmentPassword); user.UpdatedAt = now; await db.SaveChangesAsync();
        }
        return user;
    }
}
