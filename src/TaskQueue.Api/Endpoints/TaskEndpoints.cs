using TaskQueue.Api.Models;
using TaskQueue.Api.Services;

namespace TaskQueue.Api.Endpoints;

public static class TaskEndpoints
{
    public static void MapTaskEndpoints(this WebApplication app)
    {
        var group = app.MapGroup("/");

        // POST /?queue={queue} - Submit a new task command to the queue
        group.MapPost("", async (string? queue, TaskCommandRequest request, ITaskQueueService queueService) =>
        {
            if (string.IsNullOrEmpty(queue))
            {
                return Results.BadRequest(new { error = "queue query parameter is required" });
            }

            var task = await queueService.EnqueueAsync(queue, request.Command, request.Parameters);
            return Results.Created($"/{task.Id}?queue={queue}", task);
        })
        .WithName("EnqueueTask");

        // GET /poll?queue={queue} - Poll and dequeue the next task
        group.MapGet("/poll", async (string? queue, ITaskQueueService queueService, HttpContext ctx) =>
        {
            if (string.IsNullOrEmpty(queue))
            {
                return Results.BadRequest(new { error = "queue query parameter is required" });
            }

            var workerId = ctx.Request.Headers["X-Worker-Id"].FirstOrDefault();
            if (string.IsNullOrEmpty(workerId))
            {
                return Results.BadRequest(new { error = "X-Worker-Id header is required" });
            }

            var task = await queueService.DequeueAsync(queue, workerId);
            return task is null ? Results.NoContent() : Results.Ok(task);
        })
        .WithName("PollTask");

        // POST /{taskId}/ack?queue={queue} - Acknowledge successful processing
        group.MapPost("/{taskId}/ack", async (string taskId, string? queue, ITaskQueueService queueService, HttpContext ctx) =>
        {
            if (string.IsNullOrEmpty(queue))
            {
                return Results.BadRequest(new { error = "queue query parameter is required" });
            }

            var workerId = ctx.Request.Headers["X-Worker-Id"].FirstOrDefault();
            if (string.IsNullOrEmpty(workerId))
            {
                return Results.BadRequest(new { error = "X-Worker-Id header is required" });
            }

            var success = await queueService.AcknowledgeAsync(queue, taskId, workerId);
            return success ? Results.Ok() : Results.NotFound();
        })
        .WithName("AcknowledgeTask");

        // POST /{taskId}/reject?queue={queue} - Reject task (optionally requeue)
        group.MapPost("/{taskId}/reject", async (string taskId, string? queue, ITaskQueueService queueService, HttpContext ctx, bool requeue = true) =>
        {
            if (string.IsNullOrEmpty(queue))
            {
                return Results.BadRequest(new { error = "queue query parameter is required" });
            }

            var workerId = ctx.Request.Headers["X-Worker-Id"].FirstOrDefault();
            if (string.IsNullOrEmpty(workerId))
            {
                return Results.BadRequest(new { error = "X-Worker-Id header is required" });
            }

            var success = await queueService.RejectAsync(queue, taskId, workerId, requeue);
            return success ? Results.Ok() : Results.NotFound();
        })
        .WithName("RejectTask");

        // GET /processing?queue={queue} - Get tasks currently being processed by a worker
        group.MapGet("/processing", async (string? queue, ITaskQueueService queueService, HttpContext ctx) =>
        {
            if (string.IsNullOrEmpty(queue))
            {
                return Results.BadRequest(new { error = "queue query parameter is required" });
            }

            var workerId = ctx.Request.Headers["X-Worker-Id"].FirstOrDefault();
            if (string.IsNullOrEmpty(workerId))
            {
                return Results.BadRequest(new { error = "X-Worker-Id header is required" });
            }

            var tasks = await queueService.GetProcessingTasksAsync(queue, workerId);
            return Results.Ok(tasks);
        })
        .WithName("GetProcessingTasks");

        // GET /?queue={queue} - Peek at all queued tasks without removing them
        group.MapGet("", async (string? queue, ITaskQueueService queueService) =>
        {
            if (string.IsNullOrEmpty(queue))
            {
                return Results.BadRequest(new { error = "queue query parameter is required" });
            }

            var tasks = await queueService.PeekAllAsync(queue);
            return Results.Ok(tasks);
        })
        .WithName("GetAllTasks");

        // GET /count?queue={queue} - Get the number of tasks in the queue
        group.MapGet("/count", async (string? queue, ITaskQueueService queueService) =>
        {
            if (string.IsNullOrEmpty(queue))
            {
                return Results.BadRequest(new { error = "queue query parameter is required" });
            }

            var count = await queueService.GetQueueLengthAsync(queue);
            return Results.Ok(new { count });
        })
        .WithName("GetTaskCount");
    }
}
