# 🔁 Pipelines
Pipelines let you add cross-cutting behavior around request handling, for example:

* Logging
* Validation
* Metrics
* Authorization
* Caching
* Transactions
* Tracing
* Error handling

To create a custom pipeline component, implement:
```csharp
IAxentPipe<TRequest, TResponse>
```
Pipes run in the order they are registered and execute before the request handler.

## 📦 Built-in Pipes
Axent includes several pipeline features out of the box. See the [configuration](https://github.com/magmablinker/Axent/blob/main/docs/configuration.md) documentation for more details.

### 🚨 Error Handling
Enables centralized exception handling. Exceptions thrown during request processing are caught and logged depending on your configuration. Unhandled exceptions result in an Internal Server Error response.
```csharp
builder.Services.AddAxent(o => o.ErrorHandling = new AxentErrorHandlingOptions
{
    EnableDetailedExceptionResponse = true
});
```

### 📝 Request Logging
Logs incoming requests as debug entries, including execution duration and optionally the request payload.
```csharp
builder.Services.AddAxent(o => o.Logging.EnableRequestLogging = true);
```
> *Warning:* Do not use this for production environments as logs might contain sensitive data.

### 🧭 Tracing
Adds request tracing using ActivitySource.
```csharp
builder.Services.AddAxent()
    .AddTracing()
```

### 💳 Transactions
Automatically starts a transaction for requests that implement ICommand<TResponse>.
```csharp
builder.Services.AddAxent(o => o.Transactions.UseTransactions = true);
```

## 🌐 Generic Pipe
A generic pipe runs for every request type.
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
### 🛠️ Registration
```csharp
builder.Services.AddAxent()
    .AddRequestHandlers(AssemblyProvider.Current)
    .AddPipe(typeof(LoggingPipe));
```

## 🎯 Request Specific Pipe
A request-specific pipe runs only for a single request type.
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

## 📌 Notes
* Use generic pipes for behavior that should apply to all requests.
* Use request-specific pipes when the behavior is only relevant for one request type.
* Registration order matters because pipes are executed in the order they are added.
