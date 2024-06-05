using System.Threading;
using Azure.Storage.Queues.Models;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;

namespace AzureFunctionQueueTriggerCancellationTokenTest;

public class Function1
{
    private readonly ILogger<Function1> _logger;

    public Function1(ILogger<Function1> logger)
    {
        _logger = logger;
    }

    [Function(nameof(Function1))]
    public async Task Run([QueueTrigger("test-queue")] QueueMessage message, FunctionContext context, CancellationToken cancellationToken)
    {
        _logger.LogInformation($"C# Queue trigger function processed: {message.MessageText}");
        _logger.LogInformation("cancellationToken == CancellationToken.None? {IsEqual}", cancellationToken == CancellationToken.None);
        _logger.LogInformation("context.CancellationToken == CancellationToken.None? {IsEqual}", context.CancellationToken == CancellationToken.None);
        _logger.LogInformation("context.CancellationToken == cancellationToken? {IsEqual}", context.CancellationToken == cancellationToken);

        cancellationToken.Register(() =>
        {
            Console.WriteLine("CancellationToken Register callback invoked.");
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
