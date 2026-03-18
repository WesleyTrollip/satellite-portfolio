namespace SatellitePortfolio.RiskWorker;

using SatellitePortfolio.Application;

public class Worker(
    ILogger<Worker> logger,
    IServiceScopeFactory scopeFactory) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                using var scope = scopeFactory.CreateScope();
                var evaluator = scope.ServiceProvider.GetRequiredService<RiskEvaluationService>();
                var createdAlerts = await evaluator.EvaluateAndPersistAsync(DateTime.UtcNow, stoppingToken);
                logger.LogInformation("Risk evaluation completed at {time}, created {count} alerts.", DateTimeOffset.UtcNow, createdAlerts.Count);
            }
            catch (Exception exception)
            {
                logger.LogError(exception, "Risk evaluation failed at {time}.", DateTimeOffset.UtcNow);
            }

            await Task.Delay(TimeSpan.FromMinutes(15), stoppingToken);
        }
    }
}
