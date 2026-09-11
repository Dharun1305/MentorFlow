using MentorFlow.API.DTOs;

namespace MentorFlow.API.Interfaces;

public interface IMeetingService
{
    Task<IReadOnlyList<MeetingResponse>> GetAllAsync(CancellationToken cancellationToken);
    Task<MeetingResponse> GetByIdAsync(int id, CancellationToken cancellationToken);
    Task<MeetingResponse> CreateAsync(CreateMeetingRequest request, CancellationToken cancellationToken);
    Task<MeetingResponse> UpdateAsync(int id, UpdateMeetingRequest request, CancellationToken cancellationToken);
}
