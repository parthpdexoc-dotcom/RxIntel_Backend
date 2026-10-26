using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Rxintel.Generic.Services.Interfaces;

namespace Rxintel.AI.HostedServices
{
    /// <summary>
    /// Autonomous Background Service that continuously runs zero-human-interaction clinical auditing,
    /// self-training Muse AI's permanent memory bank, and auto-resolving false discrepancies in the background.
    /// </summary>
    public class MuseAutonomousLearningHostedService : BackgroundService
    {
        private static readonly TimeSpan StartupDelay = TimeSpan.FromSeconds(15);
        private static readonly TimeSpan Interval = TimeSpan.FromMinutes(15);

        private readonly IServiceScopeFactory _scopeFactory;
        private readonly ILogger<MuseAutonomousLearningHostedService> _logger;

        public MuseAutonomousLearningHostedService(
            IServiceScopeFactory scopeFactory,
            ILogger<MuseAutonomousLearningHostedService> logger)
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

            // Run initial autonomous learning sweep on application startup
            await RunAutonomousLearningSweepAsync(stoppingToken);

            using var timer = new PeriodicTimer(Interval);
            while (await timer.WaitForNextTickAsync(stoppingToken))
            {
                await RunAutonomousLearningSweepAsync(stoppingToken);
            }
        }

        private async Task RunAutonomousLearningSweepAsync(CancellationToken stoppingToken)
        {
            try
            {
                await using var scope = _scopeFactory.CreateAsyncScope();
                var learningService = scope.ServiceProvider.GetRequiredService<IAutonomousAuditLearningService>();

                _logger.LogInformation("[MuseAutoLearningService] Initiating scheduled autonomous learning sweep...");
                var report = await learningService.RunAutonomousLearningAsync(null, stoppingToken);

                if (report.AutonomouslyResolvedCount > 0)
                {
                    _logger.LogInformation(
                        "[MuseAutoLearningService] Autonomous sweep completed: {ResolvedCount} discrepancies resolved, {NewMemories} new memories learned with 0 human clicks.",
                        report.AutonomouslyResolvedCount, report.NewMemoriesLearned);
                }
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                // Graceful host shutdown
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "[MuseAutoLearningService] Autonomous learning sweep encountered an error.");
            }
        }
    }
}
