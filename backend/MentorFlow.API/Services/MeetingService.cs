using Microsoft.EntityFrameworkCore;
using MentorFlow.API.Data;
using MentorFlow.API.DTOs;
using MentorFlow.API.Entities;
using MentorFlow.API.Interfaces;

namespace MentorFlow.API.Services;

public sealed class MeetingService(MentorFlowDbContext db, ICurrentUserService currentUser) : IMeetingService
{
    public async Task<IReadOnlyList<MeetingResponse>> GetAllAsync(CancellationToken cancellationToken)
    {
        var query = ActiveQuery();
        if (currentUser.Role == "Mentor") query = query.Where(item => item.MentorStudent.Mentor.UserId == currentUser.UserId);
        else if (currentUser.Role == "Student") query = query.Where(item => item.MentorStudent.Student.UserId == currentUser.UserId);
        return (await query.OrderByDescending(item => item.MeetingAt).ToListAsync(cancellationToken)).Select(ToResponse).ToList();
    }

    public async Task<MeetingResponse> GetByIdAsync(int id, CancellationToken cancellationToken)
    {
        var meeting = await ActiveQuery().SingleOrDefaultAsync(item => item.MeetingId == id, cancellationToken)
            ?? throw new ApiException(StatusCodes.Status404NotFound, "Meeting not found.");
        EnsureAccess(meeting.MentorStudent);
        return ToResponse(meeting);
    }

    public async Task<MeetingResponse> CreateAsync(CreateMeetingRequest request, CancellationToken cancellationToken)
    {
        EnsureMeetingDate(request.MeetingAt);
        var assignment = await AssignmentQuery().SingleOrDefaultAsync(item => item.MentorStudentId == request.MentorStudentId && item.IsActive, cancellationToken)
            ?? throw new ApiException(StatusCodes.Status404NotFound, "Active mentor assignment not found.");
        EnsureCanWrite(assignment);
        var now = DateTime.UtcNow;
        var meeting = new Meeting { MentorStudentId = assignment.MentorStudentId, MeetingAt = request.MeetingAt, DurationMinutes = request.DurationMinutes, Topic = request.Topic.Trim(), Notes = request.Notes?.Trim(), Feedback = request.Feedback?.Trim(), CreatedAt = now, UpdatedAt = now, IsActive = true };
        db.Meetings.Add(meeting); await db.SaveChangesAsync(cancellationToken);
        return ToResponse(await FullQuery().SingleAsync(item => item.MeetingId == meeting.MeetingId, cancellationToken));
    }

    public async Task<MeetingResponse> UpdateAsync(int id, UpdateMeetingRequest request, CancellationToken cancellationToken)
    {
        EnsureMeetingDate(request.MeetingAt);
        var meeting = await db.Meetings.Include(item => item.MentorStudent).ThenInclude(item => item.Mentor).SingleOrDefaultAsync(item => item.MeetingId == id, cancellationToken)
            ?? throw new ApiException(StatusCodes.Status404NotFound, "Meeting not found.");
        EnsureCanWrite(meeting.MentorStudent);
        meeting.MeetingAt = request.MeetingAt; meeting.DurationMinutes = request.DurationMinutes; meeting.Topic = request.Topic.Trim(); meeting.Notes = request.Notes?.Trim(); meeting.Feedback = request.Feedback?.Trim(); meeting.IsActive = request.IsActive; meeting.UpdatedAt = DateTime.UtcNow;
        await db.SaveChangesAsync(cancellationToken);
        return ToResponse(await FullQuery().SingleAsync(item => item.MeetingId == id, cancellationToken));
    }

    private IQueryable<Meeting> FullQuery() => db.Meetings.AsNoTracking().Include(item => item.MentorStudent).ThenInclude(item => item.Mentor).ThenInclude(item => item.User).Include(item => item.MentorStudent).ThenInclude(item => item.Student).ThenInclude(item => item.User);
    private IQueryable<Meeting> ActiveQuery() => FullQuery().Where(item => item.IsActive);
    private IQueryable<MentorStudent> AssignmentQuery() => db.MentorStudents.AsNoTracking().Include(item => item.Mentor).ThenInclude(item => item.User).Include(item => item.Student).ThenInclude(item => item.User);
    private static void EnsureMeetingDate(DateTime meetingAt)
    {
        if (meetingAt == default)
            throw new ApiException(StatusCodes.Status400BadRequest, "MeetingAt is required.");
    }
    private void EnsureAccess(MentorStudent assignment) { if (currentUser.Role == "Admin" || (currentUser.Role == "Mentor" && assignment.Mentor.UserId == currentUser.UserId) || (currentUser.Role == "Student" && assignment.Student.UserId == currentUser.UserId)) return; throw new ApiException(StatusCodes.Status403Forbidden, "You do not have access to this meeting."); }
    private void EnsureCanWrite(MentorStudent assignment) { if (currentUser.Role == "Admin" || (currentUser.Role == "Mentor" && assignment.Mentor.UserId == currentUser.UserId)) return; throw new ApiException(StatusCodes.Status403Forbidden, "Only the assigned mentor or an admin can change meetings."); }
    private static MeetingResponse ToResponse(Meeting item) => new(item.MeetingId, item.MentorStudentId, item.MentorStudent.MentorId, $"{item.MentorStudent.Mentor.User.FirstName} {item.MentorStudent.Mentor.User.LastName}", item.MentorStudent.StudentId, $"{item.MentorStudent.Student.User.FirstName} {item.MentorStudent.Student.User.LastName}", item.MeetingAt, item.DurationMinutes, item.Topic, item.Notes, item.Feedback, item.IsActive);
}
