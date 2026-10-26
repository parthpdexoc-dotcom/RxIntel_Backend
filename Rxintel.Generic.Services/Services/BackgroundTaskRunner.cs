using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Rxintel.Generic.Services.Interfaces;

namespace Rxintel.Generic.Services.Services;

public class BackgroundTaskRunner : IBackgroundTaskRunner
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<BackgroundTaskRunner> _logger;

    public BackgroundTaskRunner(
        IServiceScopeFactory scopeFactory,
        ILogger<BackgroundTaskRunner> logger)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    public void Enqueue(Func<IServiceProvider, Task> work)
    {
        _ = Task.Run(async () =>
        {
            try
            {
                await using var scope = _scopeFactory.CreateAsyncScope();
                await work(scope.ServiceProvider).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Background field-mapping notification task failed.");
            }
        });
    }
}
