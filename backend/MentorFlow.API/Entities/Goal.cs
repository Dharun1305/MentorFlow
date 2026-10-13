namespace MentorFlow.API.Entities;
public sealed class Goal
{
    public byte[] RowVersion { get; set; } = [];
    public int GoalId { get; set; }
    public int MentorStudentId { get; set; }
    public string Title { get; set; } = string.Empty;
    public string? Description { get; set; }
    public DateTime? TargetDate { get; set; }
    public string Status { get; set; } = "NotStarted";
    public string Priority { get; set; } = "Medium";
    public DateTime? CompletedAt { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    public bool IsActive { get; set; }
    public MentorStudent MentorStudent { get; set; } = null!;
}
