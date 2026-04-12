using FunMasters.Data;
using FunMasters.Services;
using Microsoft.EntityFrameworkCore;

namespace FunMasters.Jobs;

public class MorningNotificationJob : BackgroundService
{
    private readonly IServiceProvider _services;
    private readonly ILogger<MorningNotificationJob> _logger;

    public MorningNotificationJob(IServiceProvider services, ILogger<MorningNotificationJob> logger)
    {
        _services = services;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        // Align to 9 AM UTC+3 (6 AM UTC), check every 15 minutes
        var now = DateTime.UtcNow;
        var target = now.Date.AddHours(6);
        if (now >= target) target = target.AddDays(1);
        await Task.Delay(target - now, stoppingToken);

        using var timer = new PeriodicTimer(TimeSpan.FromMinutes(15));

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await SendPendingAsync(stoppingToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error sending pending morning notifications");
            }

            await timer.WaitForNextTickAsync(stoppingToken);
        }
    }

    private async Task SendPendingAsync(CancellationToken stoppingToken)
    {
        using var scope = _services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var telegram = scope.ServiceProvider.GetRequiredService<TelegramService>();

        var now = DateTime.UtcNow;

        var pending = await db.PendingNotifications
            .Where(n => n.SendAfterUtc <= now)
            .OrderBy(n => n.CreatedAtUtc)
            .ToListAsync(stoppingToken);

        foreach (var notification in pending)
        {
            if (stoppingToken.IsCancellationRequested) break;

            try
            {
                // await telegram.SendMessageAsync(notification.Message);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to send pending notification {Id}, will retry", notification.Id);
                continue;
            }

            db.PendingNotifications.Remove(notification);
            await db.SaveChangesAsync(stoppingToken);
            _logger.LogInformation("Sent pending morning notification created at {CreatedAt}", notification.CreatedAtUtc);
        }
    }
}
