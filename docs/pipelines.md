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

Built-in safety stages have fixed outer ordering: authorization runs first, followed by a command
transaction or query cache when present, then custom `IAxentPipe` registrations. Registration
order still controls the order among custom pipes.

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

    public ValueTask<Response<TResponse>> ProcessAsync(
        TRequest request,
        AxentPipelineContinuation<TRequest, TResponse> next,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Handling {Request}", typeof(TRequest).Name);
        return next(request, cancellationToken);
    }
}
```
### 🛠️ Registration
```csharp
builder.Services.AddAxent()
    .AddRequestHandlersFromAssemblyContaining<ExampleQueryHandler>()
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

    public ValueTask<Response<OtherResponse>> ProcessAsync(
        OtherRequest request,
        AxentPipelineContinuation<OtherRequest, OtherResponse> next,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Running pipe for OtherRequest");
        return next(request, cancellationToken);
    }
}
```

### Registration
```csharp
builder.Services.AddAxent()
    .AddRequestHandlersFromAssemblyContaining<OtherRequestHandler>()
    .AddPipe<OtherRequestPipe>();
```

## 📌 Notes
* Use generic pipes for behavior that should apply to all requests.
* Use request-specific pipes when the behavior is only relevant for one request type.
* Registration order matters because pipes are executed in the order they are added.
* `IRequestSender<TRequest, TResponse>` is the recommended sender API for hot paths; `ISender` remains available for dynamic dispatch.
