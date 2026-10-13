using MentorFlow.API.Entities;
using MentorFlow.API.Interfaces;

namespace MentorFlow.API.Services;
internal static class MentorshipAccess
{
    public static void EnsureRead(ICurrentUserService user, MentorStudent assignment)
    {
        if (user.Role == "Admin" || (user.Role == "Mentor" && assignment.Mentor.UserId == user.UserId) || (user.Role == "Student" && assignment.Student.UserId == user.UserId))
            return;
        throw new ApiException(StatusCodes.Status403Forbidden, "You do not have access to this record.");
    }

    public static void EnsureWrite(ICurrentUserService user, MentorStudent assignment)
    {
        if (user.Role != "Admin" && !(user.Role == "Mentor" && assignment.Mentor.UserId == user.UserId))
            throw new ApiException(StatusCodes.Status403Forbidden, "Only the current assigned mentor or an admin can change this record.");
        if (!assignment.IsActive || !assignment.Mentor.IsActive || !assignment.Mentor.User.IsActive || !assignment.Student.IsActive || !assignment.Student.User.IsActive)
            throw new ApiException(StatusCodes.Status409Conflict, "Changes require an active mentor assignment and active accounts.");
    }
}
