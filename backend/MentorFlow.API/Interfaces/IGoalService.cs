using MentorFlow.API.DTOs;

namespace MentorFlow.API.Interfaces;

public interface IGoalService
{
    Task<IReadOnlyList<GoalResponse>> GetAllAsync(CancellationToken cancellationToken);
    Task<GoalResponse> GetByIdAsync(int id, CancellationToken cancellationToken);
    Task<GoalResponse> CreateAsync(CreateGoalRequest request, CancellationToken cancellationToken);
    Task<GoalResponse> UpdateAsync(int id, UpdateGoalRequest request, CancellationToken cancellationToken);
}
