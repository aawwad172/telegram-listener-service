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

    /// <summary>
    /// Initializes a new <see cref="Worker"/> instance using the provided queued message service and options monitor.
    /// </summary>
    /// <remarks>
    /// Reads the current <see cref="ListenerSettings"/>, ensures <c>ParallelWorkers</c> is at least 1, then resolves
    /// the effective number of parallel workers and the idle delay (in seconds) from the settings.
    /// </remarks>
    public Worker(IQueuedMessagesService service, IOptionsMonitor<ListenerSettings> options)
    {
        _service = service;
        _options = options.CurrentValue;
        _options.ParallelWorkers = Math.Max(1, _options.ParallelWorkers);
        _parallelWorkers = _options.ResolveWorkers();
        _idleDelaySeconds = _options.ResolveIdleDelay();
    }
    /// <summary>
    /// Starts the configured number of long-running worker loops and awaits their completion.
    /// </summary>
    /// <remarks>
    /// Each worker runs until <paramref name="stoppingToken"/> requests cancellation. This method logs startup information and returns when all worker tasks have finished (typically after cancellation).
    /// </remarks>
    /// <param name="stoppingToken">Cancellation token used to signal shutdown of the worker loops.</param>
    /// <returns>A task that completes when all worker loops have stopped.</returns>
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        // Start N long-lived worker loops (no Task.Run needed)
        Task[] tasks = Enumerable.Range(0, _parallelWorkers)
            .Select(i => RunWorkerAsync(i, stoppingToken))
            .ToArray();
        LoggerService.Info("Worker started at {Time} with {Workers} parallel workers and {IdleDelay} seconds idle delay", DateTimeOffset.Now, _parallelWorkers, _idleDelaySeconds);
        await Task.WhenAll(tasks);
    }

    /// <summary>
    /// Runs a single long‑running worker loop that repeatedly processes queued messages until cancellation is requested.
    /// </summary>
    /// <remarks>
    /// The worker repeatedly calls the queued messages service to process work. When work is processed the method logs activity; when no work is available it waits for the configured idle delay. Unexpected exceptions are logged and trigger a short backoff; OperationCanceledException driven by the provided <paramref name="cancellationToken"/> causes a clean shutdown of the loop.
    /// </remarks>
    /// <param name="workerId">Identifier for this worker instance (used in log messages).</param>
    /// <param name="cancellationToken">Token used to request graceful shutdown of the worker loop.</param>
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
