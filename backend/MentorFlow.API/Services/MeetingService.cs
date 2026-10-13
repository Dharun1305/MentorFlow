using Microsoft.EntityFrameworkCore;
using System.Data;
using MentorFlow.API.Data;
using MentorFlow.API.DTOs;
using MentorFlow.API.Entities;
using MentorFlow.API.Interfaces;

namespace MentorFlow.API.Services;
public sealed class MeetingService(MentorFlowDbContext db, ICurrentUserService currentUser) : IMeetingService
{
    public async Task<PagedResponse<MeetingResponse>> GetAllAsync(MeetingListRequest request, CancellationToken cancellationToken)
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
        if (request.From > request.To)
            throw new ApiException(StatusCodes.Status400BadRequest, "From must be before To.");
        if (request.From.HasValue)
        {
            var from = request.From.Value.UtcDateTime;
            query = query.Where(item => item.MeetingAt >= from);
        }

        if (request.To.HasValue)
        {
            var to = request.To.Value.UtcDateTime;
            query = query.Where(item => item.MeetingAt <= to);
        }

        return await query.OrderByDescending(item => item.MeetingAt).ThenByDescending(item => item.MeetingId).ToPageAsync(request, ToResponse, cancellationToken);
    }

    public async Task<MeetingResponse> GetByIdAsync(int id, CancellationToken cancellationToken)
    {
        var meeting = await ActiveQuery().SingleOrDefaultAsync(item => item.MeetingId == id, cancellationToken) ?? throw new ApiException(StatusCodes.Status404NotFound, "Meeting not found.");
        MentorshipAccess.EnsureRead(currentUser, meeting.MentorStudent);
        return ToResponse(meeting);
    }

    public async Task<MeetingResponse> CreateAsync(CreateMeetingRequest request, CancellationToken cancellationToken)
    {
        await using var transaction = db.Database.IsRelational() ? await db.Database.BeginTransactionAsync(IsolationLevel.Serializable, cancellationToken) : null;
        EnsureMeetingDate(request.MeetingAt);
        var assignment = await AssignmentQuery().SingleOrDefaultAsync(item => item.MentorStudentId == request.MentorStudentId && item.IsActive, cancellationToken) ?? throw new ApiException(StatusCodes.Status404NotFound, "Active mentor assignment not found.");
        MentorshipAccess.EnsureWrite(currentUser, assignment);
        var now = DateTime.UtcNow;
        var meeting = new Meeting
        {
            MentorStudentId = assignment.MentorStudentId,
            MeetingAt = request.MeetingAt.UtcDateTime,
            DurationMinutes = request.DurationMinutes,
            Topic = request.Topic.Trim(),
            Notes = request.Notes?.Trim(),
            Feedback = request.Feedback?.Trim(),
            CreatedAt = now,
            UpdatedAt = now,
            IsActive = true
        };
        db.Meetings.Add(meeting);
        await db.SaveChangesAsync(cancellationToken);
        if (transaction is not null)
            await transaction.CommitAsync(cancellationToken);
        return ToResponse(await FullQuery().SingleAsync(item => item.MeetingId == meeting.MeetingId, cancellationToken));
    }

    public async Task<MeetingResponse> UpdateAsync(int id, UpdateMeetingRequest request, CancellationToken cancellationToken)
    {
        await using var transaction = db.Database.IsRelational() ? await db.Database.BeginTransactionAsync(IsolationLevel.Serializable, cancellationToken) : null;
        EnsureMeetingDate(request.MeetingAt);
        var meeting = await db.Meetings
            .Include(item => item.MentorStudent)
            .ThenInclude(item => item.Mentor)
            .ThenInclude(item => item.User)
            .Include(item => item.MentorStudent)
            .ThenInclude(item => item.Student)
            .ThenInclude(item => item.User)
            .SingleOrDefaultAsync(item => item.MeetingId == id, cancellationToken) ?? throw new ApiException(StatusCodes.Status404NotFound, "Meeting not found.");
        MentorshipAccess.EnsureWrite(currentUser, meeting.MentorStudent);
        db.Entry(meeting).Property(item => item.RowVersion).OriginalValue = request.RowVersion;
        meeting.MeetingAt = request.MeetingAt.UtcDateTime;
        meeting.DurationMinutes = request.DurationMinutes;
        meeting.Topic = request.Topic.Trim();
        meeting.Notes = request.Notes?.Trim();
        meeting.Feedback = request.Feedback?.Trim();
        meeting.IsActive = request.IsActive;
        meeting.UpdatedAt = DateTime.UtcNow;
        await db.SaveChangesAsync(cancellationToken);
        if (transaction is not null)
            await transaction.CommitAsync(cancellationToken);
        return ToResponse(await FullQuery().SingleAsync(item => item.MeetingId == id, cancellationToken));
    }

    private IQueryable<Meeting> FullQuery() => db.Meetings.AsNoTracking()
            .Include(item => item.MentorStudent)
            .ThenInclude(item => item.Mentor)
            .ThenInclude(item => item.User)
            .Include(item => item.MentorStudent)
            .ThenInclude(item => item.Student)
            .ThenInclude(item => item.User);
    private IQueryable<Meeting> ActiveQuery() => FullQuery().Where(item => item.IsActive);
    private IQueryable<MentorStudent> AssignmentQuery() => db.MentorStudents.AsNoTracking()
            .Include(item => item.Mentor)
            .ThenInclude(item => item.User)
            .Include(item => item.Student)
            .ThenInclude(item => item.User);
    private static void EnsureMeetingDate(DateTimeOffset meetingAt)
    {
        if (meetingAt == default)
            throw new ApiException(StatusCodes.Status400BadRequest, "MeetingAt is required.");
    }

    private static MeetingResponse ToResponse(Meeting item) => new(item.MeetingId, item.MentorStudentId, item.MentorStudent.MentorId, $"{item.MentorStudent.Mentor.User.FirstName} {item.MentorStudent.Mentor.User.LastName}", item.MentorStudent.StudentId, $"{item.MentorStudent.Student.User.FirstName} {item.MentorStudent.Student.User.LastName}", DateTime.SpecifyKind(item.MeetingAt, DateTimeKind.Utc), item.DurationMinutes, item.Topic, item.Notes, item.Feedback, item.IsActive, item.RowVersion);
}
