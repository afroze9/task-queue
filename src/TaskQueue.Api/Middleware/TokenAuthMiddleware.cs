namespace TaskQueue.Api.Middleware;

public class TokenAuthMiddleware
{
    private readonly RequestDelegate _next;
    private readonly string? _apiToken;

    public TokenAuthMiddleware(RequestDelegate next, IConfiguration configuration)
    {
        _next = next;
        _apiToken = configuration["ApiToken"];
    }

    public async Task InvokeAsync(HttpContext context)
    {
        // Skip auth if no token is configured
        if (string.IsNullOrEmpty(_apiToken))
        {
            await _next(context);
            return;
        }

        // Skip auth for OpenAPI endpoint in development
        if (context.Request.Path.StartsWithSegments("/openapi"))
        {
            await _next(context);
            return;
        }

        var authHeader = context.Request.Headers.Authorization.FirstOrDefault();

        if (string.IsNullOrEmpty(authHeader))
        {
            context.Response.StatusCode = StatusCodes.Status401Unauthorized;
            await context.Response.WriteAsJsonAsync(new { error = "Authorization header is required" });
            return;
        }

        // Expect "Bearer <token>" format
        if (!authHeader.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase))
        {
            context.Response.StatusCode = StatusCodes.Status401Unauthorized;
            await context.Response.WriteAsJsonAsync(new { error = "Invalid authorization format. Use 'Bearer <token>'" });
            return;
        }

        var token = authHeader["Bearer ".Length..].Trim();

        if (token != _apiToken)
        {
            context.Response.StatusCode = StatusCodes.Status401Unauthorized;
            await context.Response.WriteAsJsonAsync(new { error = "Invalid token" });
            return;
        }

        await _next(context);
    }
}

public static class TokenAuthMiddlewareExtensions
{
    public static IApplicationBuilder UseTokenAuth(this IApplicationBuilder builder)
    {
        return builder.UseMiddleware<TokenAuthMiddleware>();
    }
}
