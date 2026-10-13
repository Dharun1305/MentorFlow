using Microsoft.EntityFrameworkCore;
using System.Data;
using MentorFlow.API.Data;
using MentorFlow.API.DTOs;
using MentorFlow.API.Entities;
using MentorFlow.API.Interfaces;

namespace MentorFlow.API.Services;
public sealed class AssignmentService(MentorFlowDbContext db, ICurrentUserService currentUser) : IAssignmentService
{
    public async Task<PagedResponse<AssignmentSummary>> GetAllAsync(AssignmentListRequest request, CancellationToken cancellationToken)
    {
        var query = Query();
        if (currentUser.Role == "Mentor")
            query = query.Where(item => item.Mentor.UserId == currentUser.UserId);
        else if (currentUser.Role == "Student")
            query = query.Where(item => item.Student.UserId == currentUser.UserId);
        if (request.StudentId.HasValue)
            query = query.Where(item => item.StudentId == request.StudentId.Value);
        if (request.MentorId.HasValue)
            query = query.Where(item => item.MentorId == request.MentorId.Value);
        if (request.IsActive.HasValue)
            query = query.Where(item => item.IsActive == request.IsActive.Value);
        return await query.OrderByDescending(item => item.MentorStudentId).ToPageAsync(request, ToResponse, cancellationToken);
    }

    public async Task<AssignmentSummary> CreateAsync(CreateAssignmentRequest request, CancellationToken cancellationToken)
    {
        await using var transaction = db.Database.IsRelational() ? await db.Database.BeginTransactionAsync(IsolationLevel.Serializable, cancellationToken) : null;
        var mentor = await db.Mentors.SingleOrDefaultAsync(item => item.MentorId == request.MentorId && item.IsActive && item.User.IsActive, cancellationToken) ?? throw new ApiException(StatusCodes.Status404NotFound, "Active mentor not found.");
        var student = await db.Students.SingleOrDefaultAsync(item => item.StudentId == request.StudentId && item.IsActive && item.User.IsActive, cancellationToken) ?? throw new ApiException(StatusCodes.Status404NotFound, "Active student not found.");
        if (await db.MentorStudents.AnyAsync(item => item.StudentId == student.StudentId && item.IsActive, cancellationToken))
            throw new ApiException(StatusCodes.Status409Conflict, "Student already has an active mentor assignment.");
        var now = DateTime.UtcNow;
        var assignment = new MentorStudent
        {
            MentorId = mentor.MentorId,
            StudentId = student.StudentId,
            AssignedAt = now,
            CreatedAt = now,
            UpdatedAt = now,
            IsActive = true
        };
        db.MentorStudents.Add(assignment);
        await db.SaveChangesAsync(cancellationToken);
        if (transaction is not null)
            await transaction.CommitAsync(cancellationToken);
        return ToResponse(await Query().SingleAsync(item => item.MentorStudentId == assignment.MentorStudentId, cancellationToken));
    }

    public async Task<AssignmentSummary> UpdateAsync(int id, UpdateAssignmentRequest request, CancellationToken cancellationToken)
    {
        await using var transaction = db.Database.IsRelational() ? await db.Database.BeginTransactionAsync(IsolationLevel.Serializable, cancellationToken) : null;
        var assignment = await db.MentorStudents
            .Include(item => item.Mentor)
            .ThenInclude(item => item.User)
            .Include(item => item.Student)
            .ThenInclude(item => item.User)
            .SingleOrDefaultAsync(item => item.MentorStudentId == id, cancellationToken) ?? throw new ApiException(StatusCodes.Status404NotFound, "Assignment not found.");
        if (request.IsActive && !assignment.IsActive)
            throw new ApiException(StatusCodes.Status409Conflict, "Create a new assignment to start another mentorship period.");
        if (request.IsActive && (!assignment.Mentor.IsActive || !assignment.Mentor.User.IsActive || !assignment.Student.IsActive || !assignment.Student.User.IsActive))
            throw new ApiException(StatusCodes.Status409Conflict, "Both accounts must be active.");
        if (assignment.IsActive != request.IsActive)
        {
            assignment.IsActive = request.IsActive;
            assignment.UnassignedAt = DateTime.UtcNow;
            assignment.UpdatedAt = DateTime.UtcNow;
        }

        await db.SaveChangesAsync(cancellationToken);
        if (transaction is not null)
            await transaction.CommitAsync(cancellationToken);
        return ToResponse(await Query().SingleAsync(item => item.MentorStudentId == id, cancellationToken));
    }

    private IQueryable<MentorStudent> Query() => db.MentorStudents.AsNoTracking()
            .Include(item => item.Mentor)
            .ThenInclude(mentor => mentor.User)
            .Include(item => item.Student)
            .ThenInclude(student => student.User);
    private static AssignmentSummary ToResponse(MentorStudent item) => new(item.MentorStudentId, item.MentorId, $"{item.Mentor.User.FirstName} {item.Mentor.User.LastName}", item.StudentId, $"{item.Student.User.FirstName} {item.Student.User.LastName}", DateTime.SpecifyKind(item.AssignedAt, DateTimeKind.Utc), item.IsActive);
}
