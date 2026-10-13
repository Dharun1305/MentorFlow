namespace MentorFlow.API.Entities;
public sealed class Meeting
{
    public byte[] RowVersion { get; set; } = [];
    public int MeetingId { get; set; }
    public int MentorStudentId { get; set; }
    public DateTime MeetingAt { get; set; }
    public short? DurationMinutes { get; set; }
    public string Topic { get; set; } = string.Empty;
    public string? Notes { get; set; }
    public string? Feedback { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    public bool IsActive { get; set; }
    public MentorStudent MentorStudent { get; set; } = null!;
}
