using System.Diagnostics;
using TaskQueue.Listener.Models;

namespace TaskQueue.Listener.Handlers;

public class OpenBrowserHandler : ICommandHandler
{
    private readonly ILogger<OpenBrowserHandler> _logger;

    public string CommandName => "open-browser";

    public OpenBrowserHandler(ILogger<OpenBrowserHandler> logger)
    {
        _logger = logger;
    }

    public async Task<bool> HandleAsync(TaskCommand task, CancellationToken cancellationToken = default)
    {
        var url = task.Parameters?.GetValueOrDefault("url");

        if (string.IsNullOrEmpty(url))
        {
            _logger.LogError("Task {TaskId}: Missing 'url' parameter for open-browser command", task.Id);
            return false;
        }

        _logger.LogInformation("Task {TaskId}: Opening browser with URL {Url}", task.Id, url);

        try
        {
            Process.Start(new ProcessStartInfo
            {
                FileName = url,
                UseShellExecute = true
            });

            await Task.CompletedTask;
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Task {TaskId}: Failed to open browser", task.Id);
            return false;
        }
    }
}
