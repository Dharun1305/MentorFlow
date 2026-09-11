using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using MentorFlow.API.Data;
using MentorFlow.API.DTOs;
using MentorFlow.API.Entities;
using MentorFlow.API.Interfaces;

namespace MentorFlow.API.Services;

public sealed class StudentService(
    MentorFlowDbContext db,
    IPasswordHasher<User> passwordHasher,
    ICurrentUserService currentUser) : IStudentService
{
    public async Task<IReadOnlyList<StudentResponse>> GetAllAsync(CancellationToken cancellationToken)
    {
        var query = Query();
        if (currentUser.Role == "Mentor")
            query = query.Where(student => student.MentorStudents.Any(assignment => assignment.IsActive && assignment.Mentor.UserId == currentUser.UserId));
        else if (currentUser.Role == "Student")
            query = query.Where(student => student.UserId == currentUser.UserId);
        var students = await query.ToListAsync(cancellationToken);
        return students.Select(ToResponse).ToList();
    }

    public async Task<StudentResponse> GetByIdAsync(int id, CancellationToken cancellationToken)
    {
        var student = await Query().SingleOrDefaultAsync(student => student.StudentId == id, cancellationToken)
            ?? throw new ApiException(StatusCodes.Status404NotFound, "Student not found.");
        EnsureAccess(student);
        return ToResponse(student);
    }

    public async Task<StudentResponse> CreateAsync(CreateStudentRequest request, CancellationToken cancellationToken)
    {
        var email = request.Email.Trim().ToLowerInvariant();
        if (await db.Users.AnyAsync(user => user.Email == email, cancellationToken))
            throw new ApiException(StatusCodes.Status409Conflict, "Email is already registered.");
        if (await db.Students.AnyAsync(student => student.StudentCode == request.StudentCode.Trim(), cancellationToken))
            throw new ApiException(StatusCodes.Status409Conflict, "Student code is already registered.");
        var role = await db.Roles.SingleOrDefaultAsync(role => role.Name == "Student", cancellationToken)
            ?? throw new ApiException(StatusCodes.Status500InternalServerError, "Student role is not configured.");
        var now = DateTime.UtcNow;
        var user = new User { RoleId = role.RoleId, FirstName = request.FirstName.Trim(), LastName = request.LastName.Trim(), Email = email, IsActive = true, CreatedAt = now, UpdatedAt = now };
        user.PasswordHash = passwordHasher.HashPassword(user, request.Password);
        var student = new Student { User = user, StudentCode = request.StudentCode.Trim(), IsActive = true, CreatedAt = now, UpdatedAt = now };
        db.Students.Add(student);
        await db.SaveChangesAsync(cancellationToken);
        return ToResponse(await Query().SingleAsync(item => item.StudentId == student.StudentId, cancellationToken));
    }

    public async Task<StudentResponse> UpdateAsync(int id, UpdateStudentRequest request, CancellationToken cancellationToken)
    {
        var student = await db.Students.Include(item => item.User).SingleOrDefaultAsync(item => item.StudentId == id, cancellationToken)
            ?? throw new ApiException(StatusCodes.Status404NotFound, "Student not found.");
        var email = request.Email.Trim().ToLowerInvariant();
        if (await db.Users.AnyAsync(user => user.Email == email && user.UserId != student.UserId, cancellationToken))
            throw new ApiException(StatusCodes.Status409Conflict, "Email is already registered.");
        if (await db.Students.AnyAsync(item => item.StudentCode == request.StudentCode.Trim() && item.StudentId != id, cancellationToken))
            throw new ApiException(StatusCodes.Status409Conflict, "Student code is already registered.");
        student.User.FirstName = request.FirstName.Trim(); student.User.LastName = request.LastName.Trim(); student.User.Email = email; student.User.IsActive = request.IsActive; student.User.UpdatedAt = DateTime.UtcNow;
        student.StudentCode = request.StudentCode.Trim(); student.IsActive = request.IsActive; student.UpdatedAt = DateTime.UtcNow;
        await db.SaveChangesAsync(cancellationToken);
        return ToResponse(await Query().SingleAsync(item => item.StudentId == id, cancellationToken));
    }

    public async Task DeleteAsync(int id, CancellationToken cancellationToken)
    {
        var student = await db.Students.Include(item => item.User).Include(item => item.MentorStudents).SingleOrDefaultAsync(item => item.StudentId == id, cancellationToken)
            ?? throw new ApiException(StatusCodes.Status404NotFound, "Student not found.");
        student.IsActive = false; student.User.IsActive = false; student.UpdatedAt = DateTime.UtcNow; student.User.UpdatedAt = DateTime.UtcNow;
        foreach (var assignment in student.MentorStudents.Where(item => item.IsActive)) { assignment.IsActive = false; assignment.UnassignedAt = DateTime.UtcNow; assignment.UpdatedAt = DateTime.UtcNow; }
        await db.SaveChangesAsync(cancellationToken);
    }

    private IQueryable<Student> Query() => db.Students.AsNoTracking().Include(student => student.User).Include(student => student.MentorStudents.Where(item => item.IsActive)).ThenInclude(item => item.Mentor).ThenInclude(mentor => mentor.User);
    private void EnsureAccess(Student student)
    {
        if (currentUser.Role == "Admin") return;
        if (currentUser.Role == "Student" && student.UserId == currentUser.UserId) return;
        if (currentUser.Role == "Mentor" && student.MentorStudents.Any(item => item.IsActive && item.Mentor.UserId == currentUser.UserId)) return;
        throw new ApiException(StatusCodes.Status403Forbidden, "You do not have access to this student.");
    }
    private static StudentResponse ToResponse(Student student)
    {
        var assignment = student.MentorStudents.FirstOrDefault(item => item.IsActive);
        return new StudentResponse(student.StudentId, student.UserId, student.StudentCode, student.User.FirstName, student.User.LastName, student.User.Email, student.IsActive,
            assignment is null ? null : new AssignmentSummary(assignment.MentorStudentId, assignment.MentorId, $"{assignment.Mentor.User.FirstName} {assignment.Mentor.User.LastName}", student.StudentId, $"{student.User.FirstName} {student.User.LastName}", assignment.AssignedAt, assignment.IsActive));
    }
}
