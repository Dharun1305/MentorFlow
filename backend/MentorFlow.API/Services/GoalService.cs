using Microsoft.EntityFrameworkCore;
using System.Data;
using MentorFlow.API.Data;
using MentorFlow.API.DTOs;
using MentorFlow.API.Entities;
using MentorFlow.API.Interfaces;

namespace MentorFlow.API.Services;
public sealed class GoalService(MentorFlowDbContext db, ICurrentUserService currentUser) : IGoalService
{
    public async Task<PagedResponse<GoalResponse>> GetAllAsync(GoalListRequest request, CancellationToken cancellationToken)
    {
        var query = ActiveQuery();
        if (currentUser.Role == "Mentor")
            query = query.Where(item => item.MentorStudent.Mentor.UserId == currentUser.UserId);
        else if (currentUser.Role == "Student")
            query = query.Where(item => item.MentorStudent.Student.UserId == currentUser.UserId);
        if (request.StudentId.HasValue)
            query = query.Where(item => item.MentorStudent.StudentId == request.StudentId.Value);
        if (request.MentorId.HasValue)
            query = query.Where(item => item.MentorStudent.MentorId == request.MentorId.Value);
        if (request.Status is not null)
        {
            var status = NormalizeStatus(request.Status);
            query = query.Where(item => item.Status == status);
        }

        return await query.OrderBy(item => item.TargetDate).ThenBy(item => item.GoalId).ToPageAsync(request, ToResponse, cancellationToken);
    }

    public async Task<GoalResponse> GetByIdAsync(int id, CancellationToken cancellationToken)
    {
        var goal = await ActiveQuery().SingleOrDefaultAsync(item => item.GoalId == id, cancellationToken) ?? throw new ApiException(StatusCodes.Status404NotFound, "Goal not found.");
        MentorshipAccess.EnsureRead(currentUser, goal.MentorStudent);
        return ToResponse(goal);
    }

    public async Task<GoalResponse> CreateAsync(CreateGoalRequest request, CancellationToken cancellationToken)
    {
        await using var transaction = db.Database.IsRelational() ? await db.Database.BeginTransactionAsync(IsolationLevel.Serializable, cancellationToken) : null;
        var assignment = await AssignmentQuery().SingleOrDefaultAsync(item => item.MentorStudentId == request.MentorStudentId && item.IsActive, cancellationToken) ?? throw new ApiException(StatusCodes.Status404NotFound, "Active mentor assignment not found.");
        MentorshipAccess.EnsureWrite(currentUser, assignment);
        var status = NormalizeStatus(request.Status);
        var priority = NormalizePriority(request.Priority);
        var now = DateTime.UtcNow;
        var goal = new Goal
        {
            MentorStudentId = assignment.MentorStudentId,
            Title = request.Title.Trim(),
            Description = request.Description?.Trim(),
            TargetDate = request.TargetDate?.Date,
            Status = status,
            Priority = priority,
            CompletedAt = status == "Completed" ? now : null,
            CreatedAt = now,
            UpdatedAt = now,
            IsActive = true
        };
        db.Goals.Add(goal);
        await db.SaveChangesAsync(cancellationToken);
        if (transaction is not null)
            await transaction.CommitAsync(cancellationToken);
        return ToResponse(await FullQuery().SingleAsync(item => item.GoalId == goal.GoalId, cancellationToken));
    }

    public async Task<GoalResponse> UpdateAsync(int id, UpdateGoalRequest request, CancellationToken cancellationToken)
    {
        await using var transaction = db.Database.IsRelational() ? await db.Database.BeginTransactionAsync(IsolationLevel.Serializable, cancellationToken) : null;
        var goal = await db.Goals
            .Include(item => item.MentorStudent)
            .ThenInclude(item => item.Mentor)
            .ThenInclude(item => item.User)
            .Include(item => item.MentorStudent)
            .ThenInclude(item => item.Student)
            .ThenInclude(item => item.User)
            .SingleOrDefaultAsync(item => item.GoalId == id, cancellationToken) ?? throw new ApiException(StatusCodes.Status404NotFound, "Goal not found.");
        MentorshipAccess.EnsureWrite(currentUser, goal.MentorStudent);
        db.Entry(goal).Property(item => item.RowVersion).OriginalValue = request.RowVersion;
        var status = NormalizeStatus(request.Status);
        goal.Title = request.Title.Trim();
        goal.Description = request.Description?.Trim();
        goal.TargetDate = request.TargetDate?.Date;
        goal.Status = status;
        goal.Priority = NormalizePriority(request.Priority);
        goal.CompletedAt = status == "Completed" ? goal.CompletedAt ?? DateTime.UtcNow : null;
        goal.IsActive = request.IsActive;
        goal.UpdatedAt = DateTime.UtcNow;
        await db.SaveChangesAsync(cancellationToken);
        if (transaction is not null)
            await transaction.CommitAsync(cancellationToken);
        return ToResponse(await FullQuery().SingleAsync(item => item.GoalId == id, cancellationToken));
    }

    private static string NormalizeStatus(string value)
    {
        try
        {
            return DomainValues.NormalizeStatus(value);
        }
        catch (ArgumentException exception)
        {
            throw new ApiException(StatusCodes.Status400BadRequest, exception.Message);
        }
    }

    private static string NormalizePriority(string value)
    {
        try
        {
            return DomainValues.NormalizePriority(value);
        }
        catch (ArgumentException exception)
        {
            throw new ApiException(StatusCodes.Status400BadRequest, exception.Message);
        }
    }

    private IQueryable<Goal> FullQuery() => db.Goals.AsNoTracking()
            .Include(item => item.MentorStudent)
            .ThenInclude(item => item.Mentor)
            .ThenInclude(item => item.User)
            .Include(item => item.MentorStudent)
            .ThenInclude(item => item.Student)
            .ThenInclude(item => item.User);
    private IQueryable<Goal> ActiveQuery() => FullQuery().Where(item => item.IsActive);
    private IQueryable<MentorStudent> AssignmentQuery() => db.MentorStudents.AsNoTracking()
            .Include(item => item.Mentor)
            .ThenInclude(item => item.User)
            .Include(item => item.Student)
            .ThenInclude(item => item.User);
    private static GoalResponse ToResponse(Goal item) => new(item.GoalId, item.MentorStudentId, item.MentorStudent.MentorId, $"{item.MentorStudent.Mentor.User.FirstName} {item.MentorStudent.Mentor.User.LastName}", item.MentorStudent.StudentId, $"{item.MentorStudent.Student.User.FirstName} {item.MentorStudent.Student.User.LastName}", item.Title, item.Description, item.TargetDate, DomainValues.DisplayStatus(item.Status), item.Priority, item.CompletedAt.HasValue ? DateTime.SpecifyKind(item.CompletedAt.Value, DateTimeKind.Utc) : null, item.IsActive, item.RowVersion);
}
