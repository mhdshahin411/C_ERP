namespace AegisErp.Web;

/// <summary>
/// Keeps a free-tier host (e.g. Render) from idling to sleep by periodically
/// requesting the app's own public URL, which resets the platform's inactivity
/// timer. Enabled only when the platform supplies its public URL via
/// RENDER_EXTERNAL_URL and KEEP_AWAKE is not set to "false". It is a no-op
/// locally (no such env var), so it never runs during development.
///
/// To turn it off in production without a redeploy, set the environment
/// variable KEEP_AWAKE=false in the Render dashboard and restart.
/// </summary>
public sealed class KeepAliveService : BackgroundService
{
    // Render sleeps a free service after ~15 min of no inbound traffic; ping well inside that.
    private static readonly TimeSpan Interval = TimeSpan.FromMinutes(10);

    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ILogger<KeepAliveService> _logger;
    private readonly string? _pingUrl;
    private readonly bool _enabled;

    public KeepAliveService(IHttpClientFactory httpClientFactory, ILogger<KeepAliveService> logger)
    {
        _httpClientFactory = httpClientFactory;
        _logger = logger;

        var baseUrl = Environment.GetEnvironmentVariable("RENDER_EXTERNAL_URL");
        var disabled = string.Equals(
            Environment.GetEnvironmentVariable("KEEP_AWAKE"), "false", StringComparison.OrdinalIgnoreCase);

        _enabled = !string.IsNullOrWhiteSpace(baseUrl) && !disabled;
        if (_enabled)
            _pingUrl = baseUrl!.TrimEnd('/') + "/health";
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        if (!_enabled)
        {
            _logger.LogInformation(
                "KeepAlive disabled (no RENDER_EXTERNAL_URL, or KEEP_AWAKE=false).");
            return;
        }

        _logger.LogInformation(
            "KeepAlive enabled; self-pinging {Url} every {Minutes} min.", _pingUrl, Interval.TotalMinutes);

        // Let the app finish starting before the first self-request.
        try { await Task.Delay(TimeSpan.FromSeconds(30), stoppingToken); }
        catch (OperationCanceledException) { return; }

        using var timer = new PeriodicTimer(Interval);
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                var client = _httpClientFactory.CreateClient();
                client.Timeout = TimeSpan.FromSeconds(30);
                using var response = await client.GetAsync(_pingUrl, stoppingToken);
                _logger.LogInformation(
                    "KeepAlive ping {Url} -> {Status}", _pingUrl, (int)response.StatusCode);
            }
            catch (OperationCanceledException) { break; }
            catch (Exception ex)
            {
                // Never let a failed ping crash the loop; try again next tick.
                _logger.LogWarning(ex, "KeepAlive ping failed (will retry next tick).");
            }

            try
            {
                if (!await timer.WaitForNextTickAsync(stoppingToken)) break;
            }
            catch (OperationCanceledException) { break; }
        }
    }
}
