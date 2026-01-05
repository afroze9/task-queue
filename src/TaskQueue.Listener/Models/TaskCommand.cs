namespace TaskQueue.Listener.Models;

public record TaskCommand
{
    public required string Id { get; init; }
    public required string Command { get; init; }
    public Dictionary<string, string>? Parameters { get; init; }
    public DateTime CreatedAt { get; init; }
}
