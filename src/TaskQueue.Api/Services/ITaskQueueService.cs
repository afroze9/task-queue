using TaskQueue.Api.Models;

namespace TaskQueue.Api.Services;

public interface ITaskQueueService
{
    Task<TaskCommand> EnqueueAsync(string queueName, string command, Dictionary<string, string>? parameters = null);
    Task<TaskCommand?> DequeueAsync(string queueName, string workerId);
    Task<bool> AcknowledgeAsync(string queueName, string taskId, string workerId);
    Task<bool> RejectAsync(string queueName, string taskId, string workerId, bool requeue = true);
    Task<IEnumerable<TaskCommand>> GetProcessingTasksAsync(string queueName, string workerId);
    Task<IEnumerable<TaskCommand>> PeekAllAsync(string queueName);
    Task<long> GetQueueLengthAsync(string queueName);
}
