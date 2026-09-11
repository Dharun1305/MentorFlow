namespace MentorFlow.API.Entities;

public sealed class User
{
    public int UserId { get; set; }
    public int RoleId { get; set; }
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string PasswordHash { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    public bool IsActive { get; set; }
    public Role Role { get; set; } = null!;
    public Student? Student { get; set; }
    public Mentor? Mentor { get; set; }
}
