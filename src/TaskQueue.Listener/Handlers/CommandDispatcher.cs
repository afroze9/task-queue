using TaskQueue.Listener.Models;

namespace TaskQueue.Listener.Handlers;

public class CommandDispatcher
{
    private readonly Dictionary<string, ICommandHandler> _handlers;
    private readonly ILogger<CommandDispatcher> _logger;

    public CommandDispatcher(IEnumerable<ICommandHandler> handlers, ILogger<CommandDispatcher> logger)
    {
        _handlers = handlers.ToDictionary(h => h.CommandName, StringComparer.OrdinalIgnoreCase);
        _logger = logger;
    }

    public async Task<bool> DispatchAsync(TaskCommand task, CancellationToken cancellationToken = default)
    {
        if (!_handlers.TryGetValue(task.Command, out var handler))
        {
            _logger.LogWarning("No handler registered for command: {Command}", task.Command);
            return false;
        }

        _logger.LogInformation("Dispatching task {TaskId} to handler {Handler}", task.Id, handler.GetType().Name);
        return await handler.HandleAsync(task, cancellationToken);
    }
}
