using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using MentorFlow.API.Data;
using MentorFlow.API.DTOs;
using MentorFlow.API.Entities;
using MentorFlow.API.Interfaces;

namespace MentorFlow.API.Services;

public sealed class MentorService(
    MentorFlowDbContext db,
    IPasswordHasher<User> passwordHasher,
    ICurrentUserService currentUser) : IMentorService
{
    public async Task<IReadOnlyList<MentorResponse>> GetAllAsync(CancellationToken cancellationToken)
    {
        var query = Query();
        if (currentUser.Role == "Mentor") query = query.Where(mentor => mentor.UserId == currentUser.UserId);
        else if (currentUser.Role == "Student") query = query.Where(mentor => mentor.MentorStudents.Any(item => item.IsActive && item.Student.UserId == currentUser.UserId));
        var mentors = await query.ToListAsync(cancellationToken);
        return mentors.Select(ToResponse).ToList();
    }

    public async Task<MentorResponse> GetByIdAsync(int id, CancellationToken cancellationToken)
    {
        var mentor = await Query().SingleOrDefaultAsync(item => item.MentorId == id, cancellationToken)
            ?? throw new ApiException(StatusCodes.Status404NotFound, "Mentor not found.");
        EnsureAccess(mentor);
        return ToResponse(mentor);
    }

    public async Task<MentorResponse> CreateAsync(CreateMentorRequest request, CancellationToken cancellationToken)
    {
        var email = request.Email.Trim().ToLowerInvariant();
        var code = request.EmployeeCode.Trim();
        if (await db.Users.AnyAsync(user => user.Email == email, cancellationToken)) throw new ApiException(StatusCodes.Status409Conflict, "Email is already registered.");
        if (await db.Mentors.AnyAsync(mentor => mentor.EmployeeCode == code, cancellationToken)) throw new ApiException(StatusCodes.Status409Conflict, "Employee code is already registered.");
        var role = await db.Roles.SingleOrDefaultAsync(item => item.Name == "Mentor", cancellationToken)
            ?? throw new ApiException(StatusCodes.Status500InternalServerError, "Mentor role is not configured.");
        var now = DateTime.UtcNow;
        var user = new User { RoleId = role.RoleId, FirstName = request.FirstName.Trim(), LastName = request.LastName.Trim(), Email = email, IsActive = true, CreatedAt = now, UpdatedAt = now };
        user.PasswordHash = passwordHasher.HashPassword(user, request.Password);
        db.Mentors.Add(new Mentor { User = user, EmployeeCode = code, Specialization = request.Specialization?.Trim(), IsActive = true, CreatedAt = now, UpdatedAt = now });
        await db.SaveChangesAsync(cancellationToken);
        return ToResponse(await Query().SingleAsync(item => item.EmployeeCode == code, cancellationToken));
    }

    public async Task<MentorResponse> UpdateAsync(int id, UpdateMentorRequest request, CancellationToken cancellationToken)
    {
        var mentor = await db.Mentors.Include(item => item.User).SingleOrDefaultAsync(item => item.MentorId == id, cancellationToken)
            ?? throw new ApiException(StatusCodes.Status404NotFound, "Mentor not found.");
        var email = request.Email.Trim().ToLowerInvariant(); var code = request.EmployeeCode.Trim();
        if (await db.Users.AnyAsync(user => user.Email == email && user.UserId != mentor.UserId, cancellationToken)) throw new ApiException(StatusCodes.Status409Conflict, "Email is already registered.");
        if (await db.Mentors.AnyAsync(item => item.EmployeeCode == code && item.MentorId != id, cancellationToken)) throw new ApiException(StatusCodes.Status409Conflict, "Employee code is already registered.");
        mentor.User.FirstName = request.FirstName.Trim(); mentor.User.LastName = request.LastName.Trim(); mentor.User.Email = email; mentor.User.IsActive = request.IsActive; mentor.User.UpdatedAt = DateTime.UtcNow;
        mentor.EmployeeCode = code; mentor.Specialization = request.Specialization?.Trim(); mentor.IsActive = request.IsActive; mentor.UpdatedAt = DateTime.UtcNow;
        await db.SaveChangesAsync(cancellationToken);
        return ToResponse(await Query().SingleAsync(item => item.MentorId == id, cancellationToken));
    }

    public async Task DeleteAsync(int id, CancellationToken cancellationToken)
    {
        var mentor = await db.Mentors.Include(item => item.User).Include(item => item.MentorStudents).SingleOrDefaultAsync(item => item.MentorId == id, cancellationToken)
            ?? throw new ApiException(StatusCodes.Status404NotFound, "Mentor not found.");
        mentor.IsActive = false; mentor.User.IsActive = false; mentor.UpdatedAt = DateTime.UtcNow; mentor.User.UpdatedAt = DateTime.UtcNow;
        foreach (var assignment in mentor.MentorStudents.Where(item => item.IsActive)) { assignment.IsActive = false; assignment.UnassignedAt = DateTime.UtcNow; assignment.UpdatedAt = DateTime.UtcNow; }
        await db.SaveChangesAsync(cancellationToken);
    }

    private IQueryable<Mentor> Query() => db.Mentors.AsNoTracking().Include(mentor => mentor.User).Include(mentor => mentor.MentorStudents.Where(item => item.IsActive)).ThenInclude(item => item.Student).ThenInclude(student => student.User);
    private void EnsureAccess(Mentor mentor)
    {
        if (currentUser.Role == "Admin" || (currentUser.Role == "Mentor" && mentor.UserId == currentUser.UserId) || (currentUser.Role == "Student" && mentor.MentorStudents.Any(item => item.IsActive && item.Student.UserId == currentUser.UserId))) return;
        throw new ApiException(StatusCodes.Status403Forbidden, "You do not have access to this mentor.");
    }
    private static MentorResponse ToResponse(Mentor mentor) => new(mentor.MentorId, mentor.UserId, mentor.EmployeeCode, mentor.Specialization ?? string.Empty, mentor.User.FirstName, mentor.User.LastName, mentor.User.Email, mentor.IsActive,
        mentor.MentorStudents.Select(item => new AssignmentSummary(item.MentorStudentId, mentor.MentorId, $"{mentor.User.FirstName} {mentor.User.LastName}", item.StudentId, $"{item.Student.User.FirstName} {item.Student.User.LastName}", item.AssignedAt, item.IsActive)).ToList());
}
