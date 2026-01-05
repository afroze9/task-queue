using System.Net;
using System.Net.Http.Json;
using TaskQueue.Listener.Models;

namespace TaskQueue.Listener.Services;

public class TaskQueueApiClient
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<TaskQueueApiClient> _logger;
    private readonly string _queueName;

    public TaskQueueApiClient(HttpClient httpClient, ILogger<TaskQueueApiClient> logger, IConfiguration configuration)
    {
        _httpClient = httpClient;
        _logger = logger;
        _queueName = configuration["QueueName"] ?? throw new InvalidOperationException("QueueName configuration is required");
    }

    public async Task<TaskCommand?> PollAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            var response = await _httpClient.GetAsync($"poll?queue={_queueName}", cancellationToken);

            if (response.StatusCode == HttpStatusCode.NoContent)
            {
                return null;
            }

            response.EnsureSuccessStatusCode();
            return await response.Content.ReadFromJsonAsync(TaskQueueJsonContext.Default.TaskCommand, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to poll for tasks on queue {Queue}", _queueName);
            throw;
        }
    }

    public async Task<bool> AcknowledgeAsync(string taskId, CancellationToken cancellationToken = default)
    {
        try
        {
            var response = await _httpClient.PostAsync($"{taskId}/ack?queue={_queueName}", null, cancellationToken);
            return response.IsSuccessStatusCode;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to acknowledge task {TaskId} on queue {Queue}", taskId, _queueName);
            throw;
        }
    }

    public async Task<bool> RejectAsync(string taskId, bool requeue = true, CancellationToken cancellationToken = default)
    {
        try
        {
            var response = await _httpClient.PostAsync($"{taskId}/reject?queue={_queueName}&requeue={requeue}", null, cancellationToken);
            return response.IsSuccessStatusCode;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to reject task {TaskId} on queue {Queue}", taskId, _queueName);
            throw;
        }
    }
}
