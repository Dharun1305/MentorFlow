namespace MentorFlow.API.Entities;

public sealed class MentorStudent
{
    public int MentorStudentId { get; set; }
    public int MentorId { get; set; }
    public int StudentId { get; set; }
    public DateTime AssignedAt { get; set; }
    public DateTime? UnassignedAt { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    public bool IsActive { get; set; }
    public Mentor Mentor { get; set; } = null!;
    public Student Student { get; set; } = null!;
    public ICollection<Meeting> Meetings { get; set; } = [];
    public ICollection<Goal> Goals { get; set; } = [];
}
