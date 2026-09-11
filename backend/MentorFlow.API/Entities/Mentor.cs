namespace MentorFlow.API.Entities;

public sealed class Mentor
{
    public int MentorId { get; set; }
    public int UserId { get; set; }
    public string EmployeeCode { get; set; } = string.Empty;
    public string? Specialization { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    public bool IsActive { get; set; }
    public User User { get; set; } = null!;
    public ICollection<MentorStudent> MentorStudents { get; set; } = [];
}
