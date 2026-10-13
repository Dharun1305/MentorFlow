using MentorFlow.API.Entities;

namespace MentorFlow.API.Services;
internal static class AccountLifecycle
{
    public static void SetActive(User user, IEnumerable<MentorStudent> assignments, bool active, DateTime now)
    {
        user.IsActive = active;
        user.UpdatedAt = now;
        if (active)
            return;
        foreach (var assignment in assignments.Where(item => item.IsActive))
        {
            assignment.IsActive = false;
            assignment.UnassignedAt = now;
            assignment.UpdatedAt = now;
        }
    }
}
