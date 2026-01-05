namespace TaskQueue.Api.Models;

public record TaskCommandRequest(string Command, Dictionary<string, string>? Parameters = null);
