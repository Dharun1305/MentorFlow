using MentorFlow.API.DTOs;

namespace MentorFlow.API.Interfaces;
public interface IMentorService
{
    Task<PagedResponse<MentorResponse>> GetAllAsync(ProfileListRequest request, CancellationToken cancellationToken);
    Task<MentorResponse> GetByIdAsync(int id, CancellationToken cancellationToken);
    Task<MentorResponse> CreateAsync(CreateMentorRequest request, CancellationToken cancellationToken);
    Task<MentorResponse> UpdateAsync(int id, UpdateMentorRequest request, CancellationToken cancellationToken);
    Task<MentorResponse> SetActiveAsync(int id, bool isActive, CancellationToken cancellationToken);
    Task DeleteAsync(int id, CancellationToken cancellationToken);
}
