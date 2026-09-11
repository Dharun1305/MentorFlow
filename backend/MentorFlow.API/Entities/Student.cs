namespace MentorFlow.API.Entities;

public sealed class Student
{
    public int StudentId { get; set; }
    public int UserId { get; set; }
    public string StudentCode { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    public bool IsActive { get; set; }
    public User User { get; set; } = null!;
    public ICollection<MentorStudent> MentorStudents { get; set; } = [];
}
