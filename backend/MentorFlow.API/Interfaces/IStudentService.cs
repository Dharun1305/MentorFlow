using MentorFlow.API.DTOs;

namespace MentorFlow.API.Interfaces;
public interface IStudentService
{
    Task<PagedResponse<StudentResponse>> GetAllAsync(ProfileListRequest request, CancellationToken cancellationToken);
    Task<StudentResponse> GetByIdAsync(int id, CancellationToken cancellationToken);
    Task<StudentResponse> CreateAsync(CreateStudentRequest request, CancellationToken cancellationToken);
    Task<StudentResponse> UpdateAsync(int id, UpdateStudentRequest request, CancellationToken cancellationToken);
    Task<StudentResponse> SetActiveAsync(int id, bool isActive, CancellationToken cancellationToken);
    Task DeleteAsync(int id, CancellationToken cancellationToken);
}
