using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using System.Diagnostics;
using System.Threading.Tasks;

namespace NFTMarketServer.Middleware;

public class TimeTrackingStatisticsMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<TimeTrackingStatisticsMiddleware> _logger;

    public TimeTrackingStatisticsMiddleware(RequestDelegate next, ILogger<TimeTrackingStatisticsMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        var stopwatch = Stopwatch.StartNew();

        try
        {
            await _next(context);
        }
        finally
        {
            stopwatch.Stop();
            var elapsedMilliseconds = stopwatch.ElapsedMilliseconds;
            _logger.LogInformation("TimeTrackingStatisticsMiddleware Path {path} Request took {elapsedMilliseconds} ms",
                context.Request.Path, elapsedMilliseconds);
        }
    }
}
