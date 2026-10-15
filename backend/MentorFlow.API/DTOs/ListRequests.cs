using System.ComponentModel.DataAnnotations;

namespace MentorFlow.API.DTOs;
public class PageRequest
{
    [Range(1, int.MaxValue)]
    public int Page { get; set; } = 1;

    [Range(1, 100)]
    public int PageSize { get; set; } = 20;
}

public sealed class ProfileListRequest : PageRequest
{
    [MaxLength(100)]
    public string? Search { get; set; }
    public bool? IsActive { get; set; }
}

public class AssignmentListRequest : PageRequest
{
    [MaxLength(100)] public string? Search { get; set; }
    [Range(1, int.MaxValue)]
    public int? StudentId { get; set; }

    [Range(1, int.MaxValue)]
    public int? MentorId { get; set; }
    public bool? IsActive { get; set; }
}

public sealed class MeetingListRequest : PageRequest
{
    [Range(1, int.MaxValue)]
    public int? StudentId { get; set; }

    [Range(1, int.MaxValue)]
    public int? MentorId { get; set; }
    public DateTimeOffset? From { get; set; }
    public DateTimeOffset? To { get; set; }
}

public sealed class GoalListRequest : PageRequest
{
    [Range(1, int.MaxValue)]
    public int? StudentId { get; set; }

    [Range(1, int.MaxValue)]
    public int? MentorId { get; set; }
    public string? Status { get; set; }
}

public sealed record PagedResponse<T>(IReadOnlyList<T> Items, int TotalCount, int Page, int PageSize);
