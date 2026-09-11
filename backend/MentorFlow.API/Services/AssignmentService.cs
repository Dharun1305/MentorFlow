using Microsoft.EntityFrameworkCore;
using MentorFlow.API.Data;
using MentorFlow.API.DTOs;
using MentorFlow.API.Entities;
using MentorFlow.API.Interfaces;

namespace MentorFlow.API.Services;

public sealed class AssignmentService(MentorFlowDbContext db, ICurrentUserService currentUser) : IAssignmentService
{
    public async Task<IReadOnlyList<AssignmentSummary>> GetAllAsync(CancellationToken cancellationToken)
    {
        var query = Query();
        if (currentUser.Role == "Mentor") query = query.Where(item => item.Mentor.UserId == currentUser.UserId);
        else if (currentUser.Role == "Student") query = query.Where(item => item.Student.UserId == currentUser.UserId);
        return (await query.ToListAsync(cancellationToken)).Select(ToResponse).ToList();
    }

    public async Task<AssignmentSummary> CreateAsync(CreateAssignmentRequest request, CancellationToken cancellationToken)
    {
        var mentor = await db.Mentors.SingleOrDefaultAsync(item => item.MentorId == request.MentorId && item.IsActive, cancellationToken)
            ?? throw new ApiException(StatusCodes.Status404NotFound, "Active mentor not found.");
        var student = await db.Students.SingleOrDefaultAsync(item => item.StudentId == request.StudentId && item.IsActive, cancellationToken)
            ?? throw new ApiException(StatusCodes.Status404NotFound, "Active student not found.");
        if (await db.MentorStudents.AnyAsync(item => item.StudentId == student.StudentId && item.IsActive, cancellationToken)) throw new ApiException(StatusCodes.Status409Conflict, "Student already has an active mentor assignment.");
        var existing = await db.MentorStudents.SingleOrDefaultAsync(item => item.MentorId == mentor.MentorId && item.StudentId == student.StudentId, cancellationToken);
        var now = DateTime.UtcNow;
        if (existing is not null)
        {
            existing.IsActive = true; existing.UnassignedAt = null; existing.AssignedAt = now; existing.UpdatedAt = now;
        }
        else db.MentorStudents.Add(new MentorStudent { MentorId = mentor.MentorId, StudentId = student.StudentId, AssignedAt = now, CreatedAt = now, UpdatedAt = now, IsActive = true });
        await db.SaveChangesAsync(cancellationToken);
        return ToResponse(await Query().SingleAsync(item => item.MentorId == mentor.MentorId && item.StudentId == student.StudentId, cancellationToken));
    }

    public async Task<AssignmentSummary> UpdateAsync(int id, UpdateAssignmentRequest request, CancellationToken cancellationToken)
    {
        var assignment = await db.MentorStudents.SingleOrDefaultAsync(item => item.MentorStudentId == id, cancellationToken)
            ?? throw new ApiException(StatusCodes.Status404NotFound, "Assignment not found.");
        if (request.IsActive && await db.MentorStudents.AnyAsync(item => item.StudentId == assignment.StudentId && item.IsActive && item.MentorStudentId != id, cancellationToken)) throw new ApiException(StatusCodes.Status409Conflict, "Student already has another active mentor assignment.");
        assignment.IsActive = request.IsActive; assignment.UnassignedAt = request.IsActive ? null : DateTime.UtcNow; assignment.UpdatedAt = DateTime.UtcNow;
        await db.SaveChangesAsync(cancellationToken);
        return ToResponse(await Query().SingleAsync(item => item.MentorStudentId == id, cancellationToken));
    }

    private IQueryable<MentorStudent> Query() => db.MentorStudents.AsNoTracking().Include(item => item.Mentor).ThenInclude(mentor => mentor.User).Include(item => item.Student).ThenInclude(student => student.User);
    private static AssignmentSummary ToResponse(MentorStudent item) => new(item.MentorStudentId, item.MentorId, $"{item.Mentor.User.FirstName} {item.Mentor.User.LastName}", item.StudentId, $"{item.Student.User.FirstName} {item.Student.User.LastName}", item.AssignedAt, item.IsActive);
}
