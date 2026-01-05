using TaskQueue.Listener.Handlers;
using TaskQueue.Listener.Services;

var builder = Host.CreateApplicationBuilder(args);
builder.Configuration.AddJsonFile("appsettings.json", optional: true, reloadOnChange: true);
builder.Configuration.AddJsonFile("appsettings.Development.json", optional: true, reloadOnChange: true);

// Configure HTTP client for the API
builder.Services.AddHttpClient<TaskQueueApiClient>(client =>
{
    var baseUrl = builder.Configuration["ApiBaseUrl"] ?? "https://api.afrozeamjad.com/api-tasks/";
    var token = builder.Configuration["ApiToken"] ?? "";
    var workerId = builder.Configuration["WorkerId"] ?? Environment.MachineName;

    client.BaseAddress = new Uri(baseUrl);
    client.DefaultRequestHeaders.Add("X-Worker-Id", workerId);

    if (!string.IsNullOrEmpty(token))
    {
        client.DefaultRequestHeaders.Add("Authorization", $"Bearer {token}");
    }
});

// Register command handlers
builder.Services.AddSingleton<ICommandHandler, RunScriptHandler>();
builder.Services.AddSingleton<ICommandHandler, OpenBrowserHandler>();

// Register dispatcher and polling service
builder.Services.AddSingleton<CommandDispatcher>();
builder.Services.AddHostedService<TaskPollingService>();

var host = builder.Build();
host.Run();
