using Microsoft.Azure.WebJobs;
using Microsoft.Extensions.Logging;

namespace QueueTriggerCancellationTokenTestNET6InProcess;

public class Function1
{
    [FunctionName("Function1")]
    public async Task Run([QueueTrigger("test-queue")] string myQueueItem, ILogger _logger, CancellationToken cancellationToken)
    {
        _logger.LogInformation($"C# Queue trigger function processed: {myQueueItem}");
        _logger.LogInformation("cancellationToken == CancellationToken.None? {IsEqual}", cancellationToken == CancellationToken.None);

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
