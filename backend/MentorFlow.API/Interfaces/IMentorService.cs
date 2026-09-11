using MentorFlow.API.DTOs;

namespace MentorFlow.API.Interfaces;

public interface IMentorService
{
    Task<IReadOnlyList<MentorResponse>> GetAllAsync(CancellationToken cancellationToken);
    Task<MentorResponse> GetByIdAsync(int id, CancellationToken cancellationToken);
    Task<MentorResponse> CreateAsync(CreateMentorRequest request, CancellationToken cancellationToken);
    Task<MentorResponse> UpdateAsync(int id, UpdateMentorRequest request, CancellationToken cancellationToken);
    Task DeleteAsync(int id, CancellationToken cancellationToken);
}
