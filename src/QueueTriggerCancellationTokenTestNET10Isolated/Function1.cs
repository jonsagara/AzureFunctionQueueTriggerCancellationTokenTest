using Azure.Storage.Queues.Models;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace QueueTriggerCancellationTokenTestNET9Isolated;

public class Function1
{
    private readonly ILogger<Function1> _logger;
    private readonly IHostApplicationLifetime _lifetime;

    public Function1(ILogger<Function1> logger, IHostApplicationLifetime lifetime)
    {
        _logger = logger;
        _lifetime = lifetime;
    }

    [Function(nameof(Function1))]
    public async Task Run([QueueTrigger("test-queue")] QueueMessage message, FunctionContext context, CancellationToken cancellationToken)
    {
        _logger.LogInformation($"C# Queue trigger function processed: {message.MessageText}");

        // Link the Function cancellationToken with the ApplicationStopping token to observe cancellation when the host is shutting down.
        //   This covers both the isolated worker shutting down (cancellationToken) plus the host shutting down (ApplicationStopping).
        using var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, _lifetime.ApplicationStopping);

        //_lifetime.ApplicationStopping.Register(() =>
        //{
        //    Console.WriteLine("IHostApplicationLifetime ApplicationStopping Register callback invoked.");
        //});

        try
        {
            await DoWorkAsync(linkedCts.Token);
        }
        catch (OperationCanceledException)
        {
            _logger.LogWarning("Execution canceled. Starting emergency cleanup...");
            await PerformCleanupAsync();
            _logger.LogWarning("Finished emergency cleanup.");
        }
    }


    //
    // Private methods
    //

    private async Task DoWorkAsync(CancellationToken cancellationToken)
    {
        // Simulate some work that can be cancelled.
        for (int i = 0; i < 10; i++)
        {
            cancellationToken.ThrowIfCancellationRequested();
            await Task.Delay(1000, cancellationToken); // Simulate work
        }
        foreach (var ix in Enumerable.Range(start: 0, count: 1_000))
        {
            cancellationToken.ThrowIfCancellationRequested();
            await Task.Delay(1_000, cancellationToken);

            //if (cancellationToken.IsCancellationRequested)
            //{
            //    _logger.LogInformation("Cancellation requested!");
            //    return;
            //}
            //else
            //{
            //    _logger.LogInformation("Cancellation not requested.");
            //}
        }
    }

    private async Task PerformCleanupAsync()
    {
        // Keep this fast! You likely have < 5 seconds here.
        _logger.LogInformation("Saving state to database...");
        // await _db.SaveStateAsync(...);
        _logger.LogInformation("Done.");
    }
}
