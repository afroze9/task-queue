using TaskQueue.Listener.Handlers;

namespace TaskQueue.Listener.Services;

public class TaskPollingService : BackgroundService
{
    private readonly TaskQueueApiClient _apiClient;
    private readonly CommandDispatcher _dispatcher;
    private readonly ILogger<TaskPollingService> _logger;
    private readonly IConfiguration _configuration;

    public TaskPollingService(
        TaskQueueApiClient apiClient,
        CommandDispatcher dispatcher,
        ILogger<TaskPollingService> logger,
        IConfiguration configuration)
    {
        _apiClient = apiClient;
        _dispatcher = dispatcher;
        _logger = logger;
        _configuration = configuration;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var pollingInterval = _configuration.GetValue("PollingIntervalSeconds", 5);
        var queueName = _configuration["QueueName"];
        _logger.LogInformation("Task polling service started. Polling queue '{Queue}' every {Interval} seconds", queueName, pollingInterval);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await PollAndProcessAsync(stoppingToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during polling cycle");
            }

            await Task.Delay(TimeSpan.FromSeconds(pollingInterval), stoppingToken);
        }

        _logger.LogInformation("Task polling service stopped");
    }

    private async Task PollAndProcessAsync(CancellationToken cancellationToken)
    {
        var task = await _apiClient.PollAsync(cancellationToken);

        if (task is null)
        {
            _logger.LogDebug("No tasks in queue");
            return;
        }

        _logger.LogInformation("Received task {TaskId}: {Command}", task.Id, task.Command);

        try
        {
            var success = await _dispatcher.DispatchAsync(task, cancellationToken);

            if (success)
            {
                await _apiClient.AcknowledgeAsync(task.Id, cancellationToken);
                _logger.LogInformation("Task {TaskId} completed and acknowledged", task.Id);
            }
            else
            {
                await _apiClient.RejectAsync(task.Id, requeue: true, cancellationToken);
                _logger.LogWarning("Task {TaskId} failed, requeued", task.Id);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Task {TaskId} threw an exception", task.Id);
            await _apiClient.RejectAsync(task.Id, requeue: true, cancellationToken);
        }
    }
}
