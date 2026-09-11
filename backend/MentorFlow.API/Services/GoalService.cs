using Microsoft.EntityFrameworkCore;
using MentorFlow.API.Data;
using MentorFlow.API.DTOs;
using MentorFlow.API.Entities;
using MentorFlow.API.Interfaces;

namespace MentorFlow.API.Services;

public sealed class GoalService(MentorFlowDbContext db, ICurrentUserService currentUser) : IGoalService
{
    public async Task<IReadOnlyList<GoalResponse>> GetAllAsync(CancellationToken cancellationToken)
    {
        var query = ActiveQuery();
        if (currentUser.Role == "Mentor") query = query.Where(item => item.MentorStudent.Mentor.UserId == currentUser.UserId);
        else if (currentUser.Role == "Student") query = query.Where(item => item.MentorStudent.Student.UserId == currentUser.UserId);
        return (await query.OrderBy(item => item.TargetDate).ToListAsync(cancellationToken)).Select(ToResponse).ToList();
    }

    public async Task<GoalResponse> GetByIdAsync(int id, CancellationToken cancellationToken)
    {
        var goal = await ActiveQuery().SingleOrDefaultAsync(item => item.GoalId == id, cancellationToken)
            ?? throw new ApiException(StatusCodes.Status404NotFound, "Goal not found.");
        EnsureAccess(goal.MentorStudent); return ToResponse(goal);
    }

    public async Task<GoalResponse> CreateAsync(CreateGoalRequest request, CancellationToken cancellationToken)
    {
        var assignment = await AssignmentQuery().SingleOrDefaultAsync(item => item.MentorStudentId == request.MentorStudentId && item.IsActive, cancellationToken)
            ?? throw new ApiException(StatusCodes.Status404NotFound, "Active mentor assignment not found.");
        EnsureCanWrite(assignment); var status = NormalizeStatus(request.Status); var priority = NormalizePriority(request.Priority); var now = DateTime.UtcNow;
        var goal = new Goal { MentorStudentId = assignment.MentorStudentId, Title = request.Title.Trim(), Description = request.Description?.Trim(), TargetDate = request.TargetDate?.Date, Status = status, Priority = priority, CompletedAt = status == "Completed" ? now : null, CreatedAt = now, UpdatedAt = now, IsActive = true };
        db.Goals.Add(goal); await db.SaveChangesAsync(cancellationToken);
        return ToResponse(await FullQuery().SingleAsync(item => item.GoalId == goal.GoalId, cancellationToken));
    }

    public async Task<GoalResponse> UpdateAsync(int id, UpdateGoalRequest request, CancellationToken cancellationToken)
    {
        var goal = await db.Goals.Include(item => item.MentorStudent).ThenInclude(item => item.Mentor).SingleOrDefaultAsync(item => item.GoalId == id, cancellationToken)
            ?? throw new ApiException(StatusCodes.Status404NotFound, "Goal not found.");
        EnsureCanWrite(goal.MentorStudent); var status = NormalizeStatus(request.Status); goal.Title = request.Title.Trim(); goal.Description = request.Description?.Trim(); goal.TargetDate = request.TargetDate?.Date; goal.Status = status; goal.Priority = NormalizePriority(request.Priority); goal.CompletedAt = status == "Completed" ? goal.CompletedAt ?? DateTime.UtcNow : null; goal.IsActive = request.IsActive; goal.UpdatedAt = DateTime.UtcNow;
        await db.SaveChangesAsync(cancellationToken); return ToResponse(await FullQuery().SingleAsync(item => item.GoalId == id, cancellationToken));
    }

    private static string NormalizeStatus(string value) { try { return DomainValues.NormalizeStatus(value); } catch (ArgumentException exception) { throw new ApiException(StatusCodes.Status400BadRequest, exception.Message); } }
    private static string NormalizePriority(string value) { try { return DomainValues.NormalizePriority(value); } catch (ArgumentException exception) { throw new ApiException(StatusCodes.Status400BadRequest, exception.Message); } }
    private IQueryable<Goal> FullQuery() => db.Goals.AsNoTracking().Include(item => item.MentorStudent).ThenInclude(item => item.Mentor).ThenInclude(item => item.User).Include(item => item.MentorStudent).ThenInclude(item => item.Student).ThenInclude(item => item.User);
    private IQueryable<Goal> ActiveQuery() => FullQuery().Where(item => item.IsActive);
    private IQueryable<MentorStudent> AssignmentQuery() => db.MentorStudents.AsNoTracking().Include(item => item.Mentor).ThenInclude(item => item.User).Include(item => item.Student).ThenInclude(item => item.User);
    private void EnsureAccess(MentorStudent assignment) { if (currentUser.Role == "Admin" || (currentUser.Role == "Mentor" && assignment.Mentor.UserId == currentUser.UserId) || (currentUser.Role == "Student" && assignment.Student.UserId == currentUser.UserId)) return; throw new ApiException(StatusCodes.Status403Forbidden, "You do not have access to this goal."); }
    private void EnsureCanWrite(MentorStudent assignment) { if (currentUser.Role == "Admin" || (currentUser.Role == "Mentor" && assignment.Mentor.UserId == currentUser.UserId)) return; throw new ApiException(StatusCodes.Status403Forbidden, "Only the assigned mentor or an admin can change goals."); }
    private static GoalResponse ToResponse(Goal item) => new(item.GoalId, item.MentorStudentId, item.MentorStudent.MentorId, $"{item.MentorStudent.Mentor.User.FirstName} {item.MentorStudent.Mentor.User.LastName}", item.MentorStudent.StudentId, $"{item.MentorStudent.Student.User.FirstName} {item.MentorStudent.Student.User.LastName}", item.Title, item.Description, item.TargetDate, DomainValues.DisplayStatus(item.Status), item.Priority, item.CompletedAt, item.IsActive);
}
