namespace SatellitePortfolio.MarketDataWorker;

public class Worker(ILogger<Worker> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            logger.LogInformation(
                "Market data worker heartbeat at {time}. MVP mode is no-op; use manual EOD snapshot entry.",
                DateTimeOffset.UtcNow);

            await Task.Delay(TimeSpan.FromHours(1), stoppingToken);
        }
    }
}
