using System.ComponentModel.DataAnnotations;

namespace MentorFlow.API.DTOs;
public sealed class LoginRequest
{
    [Required, EmailAddress, MaxLength(320)]
    public string Email { get; set; } = string.Empty;

    [Required, MinLength(8), MaxLength(200)]
    public string Password { get; set; } = string.Empty;
}

public sealed record UserResponse(int UserId, string FirstName, string LastName, string Email, string Role);
public sealed record LoginResponse(string Token, DateTime ExpiresAtUtc, UserResponse User);
public abstract class UserProfileRequest
{
    [Required, MaxLength(100)]
    public string FirstName { get; set; } = string.Empty;

    [Required, MaxLength(100)]
    public string LastName { get; set; } = string.Empty;

    [Required, EmailAddress, MaxLength(320)]
    public string Email { get; set; } = string.Empty;
}

public sealed class CreateStudentRequest : UserProfileRequest
{
    [Required, MinLength(8), MaxLength(200)]
    public string Password { get; set; } = string.Empty;

    [Required, MaxLength(50)]
    public string StudentCode { get; set; } = string.Empty;
}

public sealed class UpdateStudentRequest : UserProfileRequest
{
    [Required, MaxLength(50)]
    public string StudentCode { get; set; } = string.Empty;
    public bool? IsActive { get; set; }
}

public sealed record StudentResponse(int StudentId, int UserId, string StudentCode, string FirstName, string LastName, string Email, bool IsActive, AssignmentSummary? ActiveAssignment);
public sealed class CreateMentorRequest : UserProfileRequest
{
    [Required, MinLength(8), MaxLength(200)]
    public string Password { get; set; } = string.Empty;

    [Required, MaxLength(50)]
    public string EmployeeCode { get; set; } = string.Empty;

    [MaxLength(150)]
    public string? Specialization { get; set; }
}

public sealed class UpdateMentorRequest : UserProfileRequest
{
    [Required, MaxLength(50)]
    public string EmployeeCode { get; set; } = string.Empty;

    [MaxLength(150)]
    public string? Specialization { get; set; }
    public bool? IsActive { get; set; }
}

public sealed record MentorResponse(int MentorId, int UserId, string EmployeeCode, string Specialization, string FirstName, string LastName, string Email, bool IsActive, IReadOnlyList<AssignmentSummary> Assignments);
public sealed class CreateAssignmentRequest
{
    [Range(1, int.MaxValue)]
    public int MentorId { get; set; }

    [Range(1, int.MaxValue)]
    public int StudentId { get; set; }
}

public sealed class UpdateAssignmentRequest
{
    public bool IsActive { get; set; }
}

public sealed record AssignmentSummary(int MentorStudentId, int MentorId, string MentorName, int StudentId, string StudentName, DateTime AssignedAt, bool IsActive);
public sealed class CreateMeetingRequest
{
    [Range(1, int.MaxValue)]
    public int MentorStudentId { get; set; }
    public DateTimeOffset MeetingAt { get; set; }

    [Range(1, 1440)]
    public short? DurationMinutes { get; set; }

    [Required, MaxLength(200)]
    public string Topic { get; set; } = string.Empty;
    public string? Notes { get; set; }
    public string? Feedback { get; set; }
}

public sealed class UpdateMeetingRequest
{
    [Required, MinLength(8), MaxLength(8)]
    public byte[] RowVersion { get; set; } = [];
    public DateTimeOffset MeetingAt { get; set; }

    [Range(1, 1440)]
    public short? DurationMinutes { get; set; }

    [Required, MaxLength(200)]
    public string Topic { get; set; } = string.Empty;
    public string? Notes { get; set; }
    public string? Feedback { get; set; }
    public bool IsActive { get; set; } = true;
}

public sealed record MeetingResponse(int MeetingId, int MentorStudentId, int MentorId, string MentorName, int StudentId, string StudentName, DateTime MeetingAt, short? DurationMinutes, string Topic, string? Notes, string? Feedback, bool IsActive, byte[] RowVersion);
public sealed class CreateGoalRequest
{
    [Range(1, int.MaxValue)]
    public int MentorStudentId { get; set; }

    [Required, MaxLength(200)]
    public string Title { get; set; } = string.Empty;
    public string? Description { get; set; }
    public DateTime? TargetDate { get; set; }

    [Required]
    public string Status { get; set; } = string.Empty;

    [Required]
    public string Priority { get; set; } = string.Empty;
}

public sealed class UpdateGoalRequest
{
    [Required, MinLength(8), MaxLength(8)]
    public byte[] RowVersion { get; set; } = [];

    [Required, MaxLength(200)]
    public string Title { get; set; } = string.Empty;
    public string? Description { get; set; }
    public DateTime? TargetDate { get; set; }

    [Required]
    public string Status { get; set; } = string.Empty;

    [Required]
    public string Priority { get; set; } = string.Empty;
    public bool IsActive { get; set; } = true;
}

public sealed record GoalResponse(int GoalId, int MentorStudentId, int MentorId, string MentorName, int StudentId, string StudentName, string Title, string? Description, DateTime? TargetDate, string Status, string Priority, DateTime? CompletedAt, bool IsActive, byte[] RowVersion);
public sealed record ErrorResponse(string Message, object? Errors = null);

public sealed class SetProfileStatusRequest
{
    [Required] public bool? IsActive { get; set; }
}

public sealed class ReassignRequest
{
    [Range(1, int.MaxValue)] public int MentorId { get; set; }
}
