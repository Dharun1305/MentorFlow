using MentorFlow.API.DTOs;

namespace MentorFlow.API.Interfaces;

public interface IAssignmentService
{
    Task<IReadOnlyList<AssignmentSummary>> GetAllAsync(CancellationToken cancellationToken);
    Task<AssignmentSummary> CreateAsync(CreateAssignmentRequest request, CancellationToken cancellationToken);
    Task<AssignmentSummary> UpdateAsync(int id, UpdateAssignmentRequest request, CancellationToken cancellationToken);
}
