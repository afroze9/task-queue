using System.Text.Json;
using System.Text.Json.Serialization;

namespace TaskQueue.Listener.Models;

[JsonSourceGenerationOptions(PropertyNamingPolicy = JsonKnownNamingPolicy.CamelCase)]
[JsonSerializable(typeof(TaskCommand))]
public partial class TaskQueueJsonContext : JsonSerializerContext
{
}
