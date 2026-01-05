using System.Diagnostics;
using TaskQueue.Listener.Models;

namespace TaskQueue.Listener.Handlers;

public class RunScriptHandler : ICommandHandler
{
    private readonly ILogger<RunScriptHandler> _logger;
    private readonly string _scriptsFolder;

    public string CommandName => "run-script";

    public RunScriptHandler(ILogger<RunScriptHandler> logger)
    {
        _logger = logger;
        _scriptsFolder = Path.Combine(AppContext.BaseDirectory, "pwsh-scripts");
    }

    public async Task<bool> HandleAsync(TaskCommand task, CancellationToken cancellationToken = default)
    {
        var name = task.Parameters?.GetValueOrDefault("name");
        var args = task.Parameters?.GetValueOrDefault("args") ?? "";

        if (string.IsNullOrEmpty(name))
        {
            _logger.LogError("Task {TaskId}: Missing 'name' parameter for run-script command", task.Id);
            return false;
        }

        var scriptPath = Path.Combine(_scriptsFolder, $"{name}.ps1");

        if (!File.Exists(scriptPath))
        {
            _logger.LogError("Task {TaskId}: Script '{Name}' not found at {Path}", task.Id, name, scriptPath);
            return false;
        }

        _logger.LogInformation("Task {TaskId}: Executing script '{Name}' with args '{Args}'", task.Id, name, args);

        try
        {
            using var process = new Process();
            process.StartInfo = new ProcessStartInfo
            {
                FileName = "pwsh",
                Arguments = $"-NoProfile -ExecutionPolicy Bypass -File \"{scriptPath}\" {args}",
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };

            process.Start();

            var output = await process.StandardOutput.ReadToEndAsync(cancellationToken);
            var error = await process.StandardError.ReadToEndAsync(cancellationToken);

            await process.WaitForExitAsync(cancellationToken);

            if (process.ExitCode != 0)
            {
                _logger.LogError("Task {TaskId}: Script '{Name}' exited with code {ExitCode}. Error: {Error}",
                    task.Id, name, process.ExitCode, error);
                return false;
            }

            _logger.LogInformation("Task {TaskId}: Script '{Name}' completed successfully. Output: {Output}",
                task.Id, name, output);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Task {TaskId}: Failed to execute script '{Name}'", task.Id, name);
            return false;
        }
    }
}
