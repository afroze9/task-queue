namespace TaskQueue.Api.Models;

public record TaskCommand
{
    public string Id { get; init; } = Guid.NewGuid().ToString();
    public required string Command { get; init; }
    public Dictionary<string, string>? Parameters { get; init; }
    public DateTime CreatedAt { get; init; } = DateTime.UtcNow;
    public TaskCommandStatus Status { get; init; } = TaskCommandStatus.Pending;
}

public enum TaskCommandStatus
{
    Pending,
    Processing,
    Completed,
    Failed
}
