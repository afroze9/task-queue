using System.Text.Json;
using StackExchange.Redis;
using TaskQueue.Api.Models;

namespace TaskQueue.Api.Services;

public class RedisTaskQueueService : ITaskQueueService
{
    private readonly IConnectionMultiplexer _redis;
    private readonly ILogger<RedisTaskQueueService> _logger;

    public RedisTaskQueueService(IConnectionMultiplexer redis, ILogger<RedisTaskQueueService> logger)
    {
        _redis = redis;
        _logger = logger;
    }

    private static string GetQueueKey(string queueName) => $"task-queue:{queueName}:commands";
    private static string GetProcessingKey(string queueName, string workerId) => $"task-queue:{queueName}:processing:{workerId}";

    public async Task<TaskCommand> EnqueueAsync(string queueName, string command, Dictionary<string, string>? parameters = null)
    {
        var db = _redis.GetDatabase();
        var task = new TaskCommand
        {
            Command = command,
            Parameters = parameters
        };

        var json = JsonSerializer.Serialize(task);
        await db.ListRightPushAsync(GetQueueKey(queueName), json);

        _logger.LogInformation("Enqueued task {TaskId} to queue {Queue}: {Command}", task.Id, queueName, command);
        return task;
    }

    public async Task<TaskCommand?> DequeueAsync(string queueName, string workerId)
    {
        var db = _redis.GetDatabase();
        var queueKey = GetQueueKey(queueName);
        var processingKey = GetProcessingKey(queueName, workerId);

        // Atomically move from main queue to worker's processing list
        var json = await db.ListMoveAsync(queueKey, processingKey, ListSide.Left, ListSide.Right);

        if (json.IsNullOrEmpty)
        {
            return null;
        }

        var task = JsonSerializer.Deserialize<TaskCommand>(json.ToString());
        _logger.LogInformation("Dequeued task {TaskId} from queue {Queue} for worker {WorkerId}", task?.Id, queueName, workerId);
        return task;
    }

    public async Task<bool> AcknowledgeAsync(string queueName, string taskId, string workerId)
    {
        var db = _redis.GetDatabase();
        var processingKey = GetProcessingKey(queueName, workerId);

        // Find and remove the task from processing list
        var items = await db.ListRangeAsync(processingKey);
        foreach (var item in items)
        {
            if (item.IsNullOrEmpty) continue;

            var task = JsonSerializer.Deserialize<TaskCommand>(item.ToString());
            if (task?.Id == taskId)
            {
                await db.ListRemoveAsync(processingKey, item);
                _logger.LogInformation("Acknowledged task {TaskId} on queue {Queue} by worker {WorkerId}", taskId, queueName, workerId);
                return true;
            }
        }

        _logger.LogWarning("Task {TaskId} not found in processing list for queue {Queue} worker {WorkerId}", taskId, queueName, workerId);
        return false;
    }

    public async Task<bool> RejectAsync(string queueName, string taskId, string workerId, bool requeue = true)
    {
        var db = _redis.GetDatabase();
        var queueKey = GetQueueKey(queueName);
        var processingKey = GetProcessingKey(queueName, workerId);

        // Find the task in processing list
        var items = await db.ListRangeAsync(processingKey);
        foreach (var item in items)
        {
            if (item.IsNullOrEmpty) continue;

            var task = JsonSerializer.Deserialize<TaskCommand>(item.ToString());
            if (task?.Id == taskId)
            {
                await db.ListRemoveAsync(processingKey, item);

                if (requeue)
                {
                    // Put back at the front of the queue for retry
                    await db.ListLeftPushAsync(queueKey, item);
                    _logger.LogInformation("Rejected and requeued task {TaskId} on queue {Queue} by worker {WorkerId}", taskId, queueName, workerId);
                }
                else
                {
                    _logger.LogInformation("Rejected and discarded task {TaskId} on queue {Queue} by worker {WorkerId}", taskId, queueName, workerId);
                }

                return true;
            }
        }

        _logger.LogWarning("Task {TaskId} not found in processing list for queue {Queue} worker {WorkerId}", taskId, queueName, workerId);
        return false;
    }

    public async Task<IEnumerable<TaskCommand>> GetProcessingTasksAsync(string queueName, string workerId)
    {
        var db = _redis.GetDatabase();
        var processingKey = GetProcessingKey(queueName, workerId);
        var items = await db.ListRangeAsync(processingKey);

        return items
            .Where(item => !item.IsNullOrEmpty)
            .Select(item => JsonSerializer.Deserialize<TaskCommand>(item.ToString()))
            .Where(task => task != null)
            .Cast<TaskCommand>();
    }

    public async Task<IEnumerable<TaskCommand>> PeekAllAsync(string queueName)
    {
        var db = _redis.GetDatabase();
        var items = await db.ListRangeAsync(GetQueueKey(queueName));

        return items
            .Where(item => !item.IsNullOrEmpty)
            .Select(item => JsonSerializer.Deserialize<TaskCommand>(item.ToString()))
            .Where(task => task != null)
            .Cast<TaskCommand>();
    }

    public async Task<long> GetQueueLengthAsync(string queueName)
    {
        var db = _redis.GetDatabase();
        return await db.ListLengthAsync(GetQueueKey(queueName));
    }
}
