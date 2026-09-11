namespace MentorFlow.API.Interfaces;

public interface ICurrentUserService
{
    int UserId { get; }
    string Role { get; }
    bool IsAuthenticated { get; }
}
