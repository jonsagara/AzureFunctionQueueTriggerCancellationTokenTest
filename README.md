# Azure Functions v4 `QueueTrigger` doesn't receive cancellation requests

## The Problem

Inside my function, the `CancellationToken.IsCancellationRequested` property is never `true`, so I can't tell when the application is shutting down.

When running locally via Visual Studio, I tried to simulate shutting down the function by hitting `CTRL+C`, but the code that checks for cancellation is never hit.

When running a similar function in Azure, I tried to simulate shutting down by disabling the function in Azure Portal while the function was executing a long-running operation. The code that checks for and logs cancellation is never hit.

## The Question

Why can't I observe a cancellation request inside my [QueueTrigger function](https://github.com/jonsagara/AzureFunctionQueueTriggerCancellationTokenTest/blob/bf25afb42ef25006145e6d7cdd9916a7ebb2bef0/src/AzureFunctionQueueTriggerCancellationTokenTest/Function1.cs#L17)?

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

# Background

I ran into this issue because we have a long running function in Azure that will start executing, but somewhere in the middle of execution, we'll see a log message that the application is shutting down, and there are no errors or exceptions logged. The application is then left in a weird state.

```
2024-06-05 02:31:15.782 [Information] [redacted legitimate activity]
2024-06-05 02:31:15.860 [Information] [redacted legitimate activity]
2024-06-05 02:31:15.866 [Information] [redacted legitimate activity]
2024-06-05 02:31:16.233 [Information] [Microsoft.Hosting.Lifetime] Application is shutting down...
2024-06-05 02:31:31.133 [Information] [Microsoft.Hosting.Lifetime] Application started. Press Ctrl+C to shut down.
```

# Technical Details

This is a v4 Azure Function:
- Visual Studio 2022 17.11.0 Preview 1.1
- Runtime: `dotnet-isolated`
- .NET: `8.0.6`
- Function type: `QueueTrigger`
- Installed packages:
  ```
  <ItemGroup>
	<FrameworkReference Include="Microsoft.AspNetCore.App" />
	<PackageReference Include="Microsoft.Azure.Functions.Worker" Version="1.22.0" />
	<PackageReference Include="Microsoft.Azure.Functions.Worker.Extensions.Http" Version="3.2.0" />
	<PackageReference Include="Microsoft.Azure.Functions.Worker.Extensions.Http.AspNetCore" Version="1.3.1" />
	<PackageReference Include="Microsoft.Azure.Functions.Worker.Extensions.Storage.Queues" Version="5.4.0" />
	<PackageReference Include="Microsoft.Azure.Functions.Worker.Sdk" Version="1.17.2" />
	<PackageReference Include="Microsoft.ApplicationInsights.WorkerService" Version="2.22.0" />
	<PackageReference Include="Microsoft.Azure.Functions.Worker.ApplicationInsights" Version="1.2.0" />
  </ItemGroup>
  ```
