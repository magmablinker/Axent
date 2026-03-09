# 🔁 Pipelines
Pipelines allow you to add cross-cutting behavior such as:

- Logging
- Validation
- Metrics
- Authorization
- Caching

Implement
```csharp
IAxentPipe<TRequest, TResponse>
```

Pipes are executed in registration order before the handler.

## 🌐 Generic Pipe
Runs for all request types.
```csharp
internal sealed class LoggingPipe<TRequest, TResponse> : IAxentPipe<TRequest, TResponse>
{
    private readonly ILogger<LoggingPipe> _logger;

    public LoggingPipe(ILogger<LoggingPipe> logger)
    {
        _logger = logger;
    }

    public ValueTask<Response<TResponse>> ProcessAsync(IPipelineChain<TRequest, TResponse> chain, RequestContext<TRequest> context, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Handling {Request}", typeof(TRequest).Name);
        return chain.NextAsync(context, cancellationToken);
    }
}
```
### Registration
```csharp
builder.Services.AddAxent()
    .AddRequestHandlers(AssemblyProvider.Current)
    .AddPipe(typeof(LoggingPipe));
```

## 🎯 Request Specific Pipe
Runs only for a single request type.
```csharp
internal sealed class OtherRequestPipe : IAxentPipe<OtherRequest, OtherResponse>
{
    private readonly ILogger _logger;

    public OtherRequestPipe(ILogger logger)
    {
        _logger = logger;
    }

    public ValueTask<Response<OtherResponse>> ProcessAsync(IPipelineChain<OtherRequest, OtherResponse> chain, RequestContext<OtherRequest> context, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Running pipe for OtherRequest");
        return chain.NextAsync(context, cancellationToken);
    }
}
```

### Registration
```csharp
builder.Services.AddAxent()
    .AddRequestHandlers(AssemblyProvider.Current)
    .AddPipe<OtherRequestPipe>();
```
