using StackExchange.Redis;
using TaskQueue.Api.Endpoints;
using TaskQueue.Api.Middleware;
using TaskQueue.Api.Services;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddOpenApi();

// Redis
var redisConnection = builder.Configuration.GetConnectionString("Redis") ?? "localhost:6379";
builder.Services.AddSingleton<IConnectionMultiplexer>(
    ConnectionMultiplexer.Connect(redisConnection));
builder.Services.AddSingleton<ITaskQueueService, RedisTaskQueueService>();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseHttpsRedirection();
app.UseTokenAuth();
app.MapTaskEndpoints();

app.Run();
