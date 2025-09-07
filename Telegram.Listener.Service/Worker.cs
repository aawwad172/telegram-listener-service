using A2ASerilog;
using Microsoft.Extensions.Options;
using Telegram.Listener.Domain.Interfaces.Application;
using Telegram.Listener.Domain.Settings;

namespace Telegram.Listener.Service;

public class Worker : BackgroundService
{
    private readonly IQueuedMessagesService _service;
    private readonly ListenerSettings _options;
    private readonly int _parallelWorkers;
    private readonly int _idleDelaySeconds;

    public Worker(IQueuedMessagesService service, IOptionsMonitor<ListenerSettings> options)
    {
        _service = service;
        _options = options.CurrentValue;
        _options.ParallelWorkers = Math.Max(1, _options.ParallelWorkers);
        _parallelWorkers = _options.ResolveWorkers();
        _idleDelaySeconds = _options.ResolveIdleDelay();
    }
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        // Start N long-lived worker loops (no Task.Run needed)
        Task[] tasks = Enumerable.Range(0, _parallelWorkers)
            .Select(i => RunWorkerAsync(i, stoppingToken))
            .ToArray();
        LoggerService.Info("Worker started at {Time} with {Workers} parallel workers and {IdleDelay} seconds idle delay", DateTimeOffset.Now, _parallelWorkers, _idleDelaySeconds);
        await Task.WhenAll(tasks);
    }

    private async Task RunWorkerAsync(
        int workerId,
        CancellationToken cancellationToken)
    {
        LoggerService.Info("Worker {Id} started", workerId);

        while (!cancellationToken.IsCancellationRequested)
        {
            try
            {
                bool didWork = await _service.ProcessQueuedMessagesAsync(cancellationToken);
                if (didWork)
                {
                    LoggerService.Info("Worker {Id}: processed work", workerId);
                    LoggerService.Info("All workers stopped at {Time}", DateTimeOffset.Now);
                }
                else
                {
                    LoggerService.Info("Worker {Id}: idle, sleeping {Idle}s", workerId, _idleDelaySeconds);
                    if (_idleDelaySeconds > 0)
                        await Task.Delay(TimeSpan.FromSeconds(_idleDelaySeconds), cancellationToken);
                }
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                // normal shutdown
                break;
            }
            catch (Exception ex)
            {
                // Don't let one error kill the loop
                LoggerService.Error("Worker {Id} error: {Message}", workerId, ex.Message);
                // Small backoff after unexpected errors
                if (TimeSpan.FromSeconds(_idleDelaySeconds) > TimeSpan.Zero)
                    await Task.Delay(TimeSpan.FromSeconds(_idleDelaySeconds), cancellationToken);
            }
        }

        LoggerService.Info("Worker {Id} stopping", workerId);
    }
}
