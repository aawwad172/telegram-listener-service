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
        var tasks = Enumerable.Range(0, _parallelWorkers)
            .Select(i => RunWorkerAsync(i, stoppingToken))
            .ToArray();

        await Task.WhenAll(tasks);
    }

    private async Task RunWorkerAsync(int workerId, CancellationToken ct)
    {
        LoggerService.Info("Worker {Id} started", workerId);

        while (!ct.IsCancellationRequested)
        {
            try
            {
                // Recommended service shape: process ONE file per call
                // Returns true if it did work, false if nothing available
                await _service.ProcessQueuedMessagesAsync(ct);
            }
            catch (OperationCanceledException) when (ct.IsCancellationRequested)
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
                    await Task.Delay(_idleDelaySeconds, ct);
            }
        }

        LoggerService.Info("Worker {Id} stopping", workerId);
    }
}
