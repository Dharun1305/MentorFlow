namespace MentorFlow.API.DTOs;

public static class DomainValues
{
    public static readonly string[] GoalStatuses = ["NotStarted", "InProgress", "Completed"];
    public static readonly string[] GoalPriorities = ["Low", "Medium", "High"];

    public static string NormalizeStatus(string value) => value.Trim().Replace(" ", string.Empty) switch
    {
        "NotStarted" => "NotStarted",
        "InProgress" => "InProgress",
        "Completed" => "Completed",
        _ => throw new ArgumentException("Status must be Not Started, In Progress, or Completed.")
    };

    public static string NormalizePriority(string value) => value.Trim() switch
    {
        "Low" => "Low",
        "Medium" => "Medium",
        "High" => "High",
        _ => throw new ArgumentException("Priority must be Low, Medium, or High.")
    };

    public static string DisplayStatus(string value) => value switch
    {
        "NotStarted" => "Not Started",
        "InProgress" => "In Progress",
        _ => value
    };
}
