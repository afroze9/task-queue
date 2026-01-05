using TaskQueue.Listener.Models;

namespace TaskQueue.Listener.Handlers;

public interface ICommandHandler
{
    string CommandName { get; }
    Task<bool> HandleAsync(TaskCommand task, CancellationToken cancellationToken = default);
}
