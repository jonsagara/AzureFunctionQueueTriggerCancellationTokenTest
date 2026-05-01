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
        _logger.LogInformation("cancellationToken == CancellationToken.None? {IsEqual}", cancellationToken == CancellationToken.None);
        _logger.LogInformation("context.CancellationToken == CancellationToken.None? {IsEqual}", context.CancellationToken == CancellationToken.None);
        _logger.LogInformation("context.CancellationToken == cancellationToken? {IsEqual}", context.CancellationToken == cancellationToken);

        _lifetime.ApplicationStopping.Register(() =>
        {
            Console.WriteLine("IHostApplicationLifetime ApplicationStopping Register callback invoked.");
        });

        cancellationToken.Register(() =>
        {
            Console.WriteLine("CancellationToken Register callback invoked.");
        });

        context.CancellationToken.Register(() =>
        {
            Console.WriteLine("FunctionContext CancellationToken Register callback invoked.");
        });

        foreach (var ix in Enumerable.Range(start: 0, count: 1_000))
        {
            await Task.Delay(1_000);

            if (cancellationToken.IsCancellationRequested)
            {
                _logger.LogInformation("Cancellation requested!");
                return;
            }
            else
            {
                _logger.LogInformation("Cancellation not requested.");
            }
        }
    }
}
