using Azure.Storage.Queues.Models;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace QueueTriggerCancellationTokenTestNET9Isolated;

public class Function1
{
    private readonly ILogger<Function1> _logger;

    public Function1(ILogger<Function1> logger)
    {
        _logger = logger;
    }

    [Function(nameof(Function1))]
    public async Task Run([QueueTrigger("test-queue")] QueueMessage message, FunctionContext context, CancellationToken cancellationToken, IHostApplicationLifetime hostAppLifetime)
    {
        _logger.LogInformation($"C# Queue trigger function processed: {message.MessageText}");
        _logger.LogInformation("cancellationToken == CancellationToken.None? {IsEqual}", cancellationToken == CancellationToken.None);
        _logger.LogInformation("context.CancellationToken == CancellationToken.None? {IsEqual}", context.CancellationToken == CancellationToken.None);
        _logger.LogInformation("context.CancellationToken == cancellationToken? {IsEqual}", context.CancellationToken == cancellationToken);

        if (hostAppLifetime is not null)
        {
            _logger.LogInformation("hostAppLifetime.ApplicationStopping == cancellationToken? {IsEqual}", hostAppLifetime.ApplicationStopping == cancellationToken);
        }
        else
        {
            _logger.LogInformation("hostAppLifetime is null");
        }

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
