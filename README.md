# Azure Functions v4 `QueueTrigger` doesn't receive cancellation requests

## The Problem

Inside my .NET 8 Isolated function, the `CancellationToken.IsCancellationRequested` property is never `true`, so I can't tell when the application is shutting down.

When running locally via Visual Studio, I tried to simulate shutting down the function by hitting `CTRL+C`, but the code that checks for cancellation is never hit.

When running a similar function in Azure, I tried to simulate shutting down by disabling the function in Azure Portal while the function was executing a long-running operation. The code that checks for and logs cancellation is never hit.

## The Question

Why can't I observe a cancellation request inside my [QueueTrigger function](https://github.com/jonsagara/AzureFunctionQueueTriggerCancellationTokenTest/blob/a9a3c0e70406373730295b5f42c687d62e5f7799/src/QueueTriggerCancellationTokenTestNET8Isolated/Function1.cs#L18)?

```csharp
[Function(nameof(Function1))]
public async Task Run([QueueTrigger("test-queue")] QueueMessage message, CancellationToken cancellationToken)
{
    _logger.LogInformation($"C# Queue trigger function processed: {message.MessageText}");
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
```

### Output

When running the `QueueTriggerCancellationTokenTestNET8Isolated` function, I get the following output:

```
[2024-06-13T21:32:36.207Z] Host lock lease acquired by instance ID '0000000000000000000000005864DAB7'.
[2024-06-13T21:32:39.153Z] Executing 'Functions.Function1' (Reason='New queue message detected on 'test-queue'.', Id=a33717ef-5fa9-4d3a-8c25-bc04502b9722)
[2024-06-13T21:32:39.156Z] Trigger Details: MessageId: c438524d-2a36-4b56-9aa5-ca9f56f0ed9e, DequeueCount: 1, InsertedOn: 2024-06-13T21:32:39.000+00:00
[2024-06-13T21:32:39.243Z] context.CancellationToken == cancellationToken? True
[2024-06-13T21:32:39.243Z] context.CancellationToken == CancellationToken.None? False
[2024-06-13T21:32:39.243Z] hostAppLifetime is null
[2024-06-13T21:32:39.243Z] C# Queue trigger function processed: { "name": "jon" }
[2024-06-13T21:32:39.243Z] cancellationToken == CancellationToken.None? False
[2024-06-13T21:32:40.242Z] Cancellation not requested.
[2024-06-13T21:32:41.245Z] Cancellation not requested.
[2024-06-13T21:32:42.251Z] Cancellation not requested.
[2024-06-13T21:32:43.255Z] Cancellation not requested.
[2024-06-13T21:32:44.259Z] Cancellation not requested.
[2024-06-13T21:32:45.264Z] Cancellation not requested.
[2024-06-13T21:32:46.270Z] Cancellation not requested.
[2024-06-13T21:32:47.276Z] Cancellation not requested.
[2024-06-13T21:32:48.282Z] Cancellation not requested.
[2024-06-13T21:32:49.290Z] Cancellation not requested.
[2024-06-13T21:32:50.294Z] Cancellation not requested.
[2024-06-13T21:32:50.828Z] Host did not shutdown within its allotted time.
```

`cancellationToken.IsCancellationRequested` is never true.

# Other Versions

## .NET 6 Isolated

When running `QueueTriggerCancellationTokenTestNET6Isolated`, I get the following output:

```
[2024-06-13T21:35:35.192Z] Host lock lease acquired by instance ID '0000000000000000000000005864DAB7'.
[2024-06-13T21:35:36.088Z] Executing 'Functions.Function1' (Reason='New queue message detected on 'test-queue'.', Id=591cb885-3d07-42f0-9cda-0167b5018829)
[2024-06-13T21:35:36.095Z] Trigger Details: MessageId: 21470f39-e98b-49d9-a509-96761ec3ed2e, DequeueCount: 1, InsertedOn: 2024-06-13T21:35:36.000+00:00
[2024-06-13T21:35:36.204Z] context.CancellationToken == CancellationToken.None? False
[2024-06-13T21:35:36.204Z] hostAppLifetime is null
[2024-06-13T21:35:36.204Z] cancellationToken == CancellationToken.None? False
[2024-06-13T21:35:36.204Z] C# Queue trigger function processed: { "name": "jon" }
[2024-06-13T21:35:36.204Z] context.CancellationToken == cancellationToken? True
[2024-06-13T21:35:37.207Z] Cancellation not requested.
[2024-06-13T21:35:38.212Z] Cancellation not requested.
[2024-06-13T21:35:39.233Z] Cancellation not requested.
[2024-06-13T21:35:40.243Z] Cancellation not requested.
[2024-06-13T21:35:41.250Z] Cancellation not requested.
[2024-06-13T21:35:42.255Z] Cancellation not requested.
[2024-06-13T21:35:43.259Z] Cancellation not requested.
[2024-06-13T21:35:44.279Z] Cancellation not requested.
[2024-06-13T21:35:45.284Z] Cancellation not requested.
[2024-06-13T21:35:46.289Z] Cancellation not requested.
[2024-06-13T21:35:47.293Z] Cancellation not requested.
[2024-06-13T21:35:47.654Z] Host did not shutdown within its allotted time.
[2024-06-13T21:35:47.663Z] FunctionContext CancellationToken Register callback invoked.
[2024-06-13T21:35:47.669Z] CancellationToken Register callback invoked.
```

`cancellationToken.IsCancellationRequested` is never true inside the loop, but you can see that the `CancellationToken.Register` callback is eventually invoked just before the function completes.

## .NET 6 In-process

When running `QueueTriggerCancellationTokenTestNET6InProcess`, I get the following output:

```
[2024-06-13T21:37:21.067Z] Host lock lease acquired by instance ID '0000000000000000000000005864DAB7'.
[2024-06-13T21:37:22.696Z] Executing 'Function1' (Reason='New queue message detected on 'test-queue'.', Id=42cf89d3-c1a1-49ff-a75a-875821b11d30)
[2024-06-13T21:37:22.698Z] Trigger Details: MessageId: b5c4ed8b-413e-4345-953e-d63d96b3a5b0, DequeueCount: 1, InsertedOn: 2024-06-13T21:37:21.000+00:00
[2024-06-13T21:37:22.709Z] C# Queue trigger function processed: { "name": "jon" }
[2024-06-13T21:37:22.710Z] cancellationToken == CancellationToken.None? False
[2024-06-13T21:37:23.719Z] Cancellation not requested.
CancellationToken Register callback invoked.
[2024-06-13T21:37:24.723Z] Cancellation requested!
[2024-06-13T21:37:24.753Z] Executed 'Function1' (Succeeded, Id=42cf89d3-c1a1-49ff-a75a-875821b11d30, Duration=2078ms)
```

**It works in the .NET 6 in-process model.**

Immediately after I press `CTRL+C`, you can see that `cancellationToken.IsCancellationRequested` is `true`, and the loop exits.

# Background

I ran into this issue because we have a long running function in Azure that will start executing, but somewhere in the middle of execution, we'll see a log message that the application is shutting down, and there are no errors or exceptions logged. The application is then left in a weird state.

```
2024-06-05 02:31:15.782 [Information] [redacted legitimate activity, 1/250]
2024-06-05 02:31:15.860 [Information] [redacted legitimate activity, 2/250]
2024-06-05 02:31:15.866 [Information] [redacted legitimate activity, 3/250]
2024-06-05 02:31:16.233 [Information] [Microsoft.Hosting.Lifetime] Application is shutting down...
2024-06-05 02:31:31.133 [Information] [Microsoft.Hosting.Lifetime] Application started. Press Ctrl+C to shut down.
```

# Related

- [Stack Overflow question](https://stackoverflow.com/q/78578960)
- [GitHub issue](https://github.com/Azure/azure-functions-dotnet-worker/issues/2510)
- [Documentation about using CancellationToken for graceful shutdown in the isolated worker model](https://learn.microsoft.com/en-us/azure/azure-functions/dotnet-isolated-process-guide?tabs=windows#cancellation-tokens)

# Technical Details

This is a v4 Azure Function:
- Visual Studio 2022 17.11.0 Preview 3.0
- Runtimes: both `dotnet-isolated` and `dotnet`
- .NET: `8.0.7`, `6.0.32`
- Function type: `QueueTrigger`

**NOTE**: For `QueueTriggerCancellationTokenTestNET8Isolated` and `QueueTriggerCancellationTokenTestNET6Isolated`, you'll need to add a `local.settings.json` file to the project root with the following:

```json
{
  "IsEncrypted": false,
  "Values": {
    "AzureWebJobsStorage": "{actual Azure Storage connection string}",
    "FUNCTIONS_WORKER_RUNTIME": "dotnet-isolated"
  }
}
```

For `QueueTriggerCancellationTokenTestNET6InProcess`, you'll need to add a `local.settings.json` file to the project root with the following:

```json
{
  "IsEncrypted": false,
  "Values": {
    "AzureWebJobsStorage": "{actual Azure Storage connection string}",
    "FUNCTIONS_WORKER_RUNTIME": "dotnet"
  }
}
```
