using FunMasters.Services;

namespace FunMasters.Jobs;
public class QueueManagerJob : BackgroundService
{
    private readonly IServiceProvider _services;
    private readonly ILogger<QueueManagerJob> _logger;
    private static readonly TimeSpan Interval = TimeSpan.FromHours(1);

    public QueueManagerJob(IServiceProvider services, ILogger<QueueManagerJob> logger)
    {
        _services = services;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                using var scope = _services.CreateScope();
                var manager = scope.ServiceProvider.GetRequiredService<QueueManager>();
                await manager.UpdateQueueAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating queue");
            }

            await Task.Delay(Interval, stoppingToken);
        }
    }



}
