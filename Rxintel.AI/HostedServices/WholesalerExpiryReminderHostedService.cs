using Rxintel.Generic.Services.Interfaces;

namespace Rxintel.AI.HostedServices;

/// <summary>
/// Daily job: compare each wholesaler expiry date with today and email admins
/// on the exact 1-month / 1-week / 1-day before and 1-month after dates.
/// </summary>
public class WholesalerExpiryReminderHostedService : BackgroundService
{
    private static readonly TimeSpan StartupDelay = TimeSpan.FromSeconds(30);
    private static readonly TimeSpan CheckInterval = TimeSpan.FromHours(1);

    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<WholesalerExpiryReminderHostedService> _logger;
    private DateTime? _lastRunDate;

    public WholesalerExpiryReminderHostedService(
        IServiceScopeFactory scopeFactory,
        ILogger<WholesalerExpiryReminderHostedService> logger)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        try
        {
            await Task.Delay(StartupDelay, stoppingToken);
        }
        catch (OperationCanceledException)
        {
            return;
        }

        await RunIfNewDayAsync(stoppingToken);

        using var timer = new PeriodicTimer(CheckInterval);
        while (await timer.WaitForNextTickAsync(stoppingToken))
            await RunIfNewDayAsync(stoppingToken);
    }

    private async Task RunIfNewDayAsync(CancellationToken stoppingToken)
    {
        var today = DateTime.Today;
        if (_lastRunDate == today)
            return;

        try
        {
            await using var scope = _scopeFactory.CreateAsyncScope();
            var notificationService = scope.ServiceProvider.GetRequiredService<IWholesalerNotificationService>();
            await notificationService.SendDueExpiryRemindersAsync(stoppingToken);
            _lastRunDate = today;
        }
        catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
        {
            // Host is shutting down.
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Wholesaler VAWD expiry reminder job failed.");
        }
    }
}
