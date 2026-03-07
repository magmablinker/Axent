# Axent

![Axent Logo](https://raw.githubusercontent.com/magmablinker/Axent/refs/heads/main/misc/axent-logo.svg)

[![NuGet](https://img.shields.io/nuget/v/Axent.Abstractions?label=Axent.Abstractions)](https://www.nuget.org/packages/Axent.Abstractions)
[![NuGet](https://img.shields.io/nuget/v/Axent.Core?label=Axent.Core)](https://www.nuget.org/packages/Axent.Core)
[![NuGet](https://img.shields.io/nuget/v/Axent.Extensions.AspNetCore?label=Axent.Extensions.AspNetCore)](https://www.nuget.org/packages/Axent.Extensions.AspNetCore)
[![Downloads](https://img.shields.io/nuget/dt/Axent.Core.svg)](https://www.nuget.org/packages/Axent.Core/)
[![License](https://img.shields.io/badge/license-APACHE-blue)](LICENSE)

**Axent** is a lightweight, high-performance .NET library for implementing CQRS patterns with minimal boilerplate. It provides a simple request/response pipeline. It is currently ~2x faster than MediatR.

---

## Features
- Minimal setup for CQRS
- Minimal allocations
- Simple dependency injection
- ASP.NET Core integration
- Typed pipelines
- Optimized for performance and simplicity

---

## Prerequisites
- .NET 8 or later

## Getting started
### 1. Install Packages
```shell
dotnet add package Axent.Core --version 1.1.0
dotnet add package Axent.Extensions.AspNetCore --version 1.1.0
```

### 2. Register Services
```csharp
builder.Services.AddHttpContextAccessor()
builder.Services.AddAxent()
    .AddRequestHandlers(AssemblyProvider.Current);
```

### 3. Implement a Request Handler
*Example showing a request that logs a message*
```csharp
using Axent.Abstractions;
using Axent.Core;

namespace Axent.ExampleApi;

internal sealed class ExampleRequest : IRequest<Unit>
{
    public required string Message { get; init; }
}

internal sealed class ExampleRequestHandler : RequestHandler<ExampleRequest, Unit>
{
    private readonly ILogger<ExampleRequestHandler> _logger;

    public ExampleRequestHandler(ILogger<ExampleRequestHandler> logger)
    {
        _logger = logger;
    }

    public override ValueTask<Response<Unit>> HandleAsync(RequestContext<ExampleRequest> context, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Message from request '{0}'", context.Request.Message);
        return ValueTask.FromResult(Response.Success(Unit.Value));
    }
}
```

### 4. Call the Request Handler in an API Endpoint
```csharp
app.MapGet("/api/example", async (ISender sender, CancellationToken cancellationToken) =>
{
    var request = new ExampleRequest
    {
        Message = "Hello World!"
    };

    var response = await sender.SendAsync(request, cancellationToken);
    return response.ToResult();
});
```

## Pipelines
Axent allows you to add custom processors to your request pipeline by implementing `IAxentPipe<TRequest, TResponse>`. This is useful for logging, validation, metrics, or any cross-cutting concerns.
### Generic Pipe
```csharp
internal sealed class ExampleRequestPipe<TRequest, TResponse> : IAxentPipe<TRequest, TResponse>
{
    private readonly ILogger<ExampleRequestPipe<TRequest, TResponse>> _logger;

    public ExampleRequestPipe(ILogger<ExampleRequestPipe<TRequest, TResponse>> logger)
    {
        _logger = logger;
    }

    public ValueTask<Response<TResponse>> ProcessAsync(IPipelineChain<TRequest, TResponse> chain, RequestContext<TRequest> context, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("This pipe runs during every request.");
        return chain.NextAsync(context, cancellationToken);
    }
}

builder.Services.AddAxent()
    .AddRequestHandlers(AssemblyProvider.Current)
    .AddPipe(typeof(ExampleRequestPipe<,>));
```
> This pipe executes for every request handled by Axent.

### Specific Pipe
```csharp
internal sealed class OtherRequestPipe : IAxentPipe<OtherRequest, OtherResponse>
{
    private readonly ILogger<OtherRequestPipe> _logger;

    public OtherRequestPipe(ILogger<OtherRequestPipe> logger)
    {
        _logger = logger;
    }

    public ValueTask<Response<OtherResponse>> ProcessAsync(IPipelineChain<OtherRequest, OtherResponse> chain, RequestContext<OtherRequest> context, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("I only run during other request");
        return chain.NextAsync(context, cancellationToken);
    }
}

builder.Services.AddAxent()
    .AddRequestHandlers(AssemblyProvider.Current)
    .AddPipe<OtherRequestPipe>();
```
> This pipe executes for every request of the type `OtherRequest`

## Options

### `AxentOptions`

AxentOptions allows you to configure optional settings that modify the behavior of the Axent library.

```csharp
public sealed class AxentOptions
{
    /// <summary>
    /// Determines whether error handling is enabled or exceptions should be "forwarded" to the consumer
    /// of the library.
    /// No errors will be caught if the options are not set.
    /// </summary>
    public AxentErrorHandlingOptions? ErrorHandling { get; set; }
}

public sealed class AxentErrorHandlingOptions
{
    /// <summary>
    /// Determines whether the full exception gets returned or not, when the `ErrorHandlingPipe` is registered.
    /// Defaults to false
    /// </summary>
    public bool EnableDetailedExceptionResponse { get; set; }
}

```

## Benchmark

### Source Generated Dispatch
```
BenchmarkDotNet v0.14.0, Windows 11 (10.0.26200.7840)
Unknown processor
.NET SDK 10.0.200-preview.0.26103.119
  [Host]     : .NET 8.0.23 (8.0.2325.60607), X64 RyuJIT AVX-512F+CD+BW+DQ+VL+VBMI [AttachedDebugger]
  DefaultJob : .NET 8.0.23 (8.0.2325.60607), X64 RyuJIT AVX-512F+CD+BW+DQ+VL+VBMI
```
| Method                            | Mean     | Error    | StdDev   | Ratio | RatioSD | Gen0   | Allocated | Alloc Ratio |
|---------------------------------- |---------:|---------:|---------:|------:|--------:|-------:|----------:|------------:|
| &#39;SendAsync (cold)&#39;                | 36.74 ns | 0.741 ns | 1.702 ns |  1.00 |    0.06 | 0.0196 |     328 B |        1.00 |
| &#39;SendAsync (warm, same instance)&#39; | 33.97 ns | 0.423 ns | 0.353 ns |  0.93 |    0.04 | 0.0181 |     304 B |        0.93 |


### MediatR (v12.5.0)
```
BenchmarkDotNet v0.14.0, Windows 11 (10.0.26200.7840)
Unknown processor
.NET SDK 10.0.200-preview.0.26103.119
  [Host]     : .NET 8.0.23 (8.0.2325.60607), X64 RyuJIT AVX-512F+CD+BW+DQ+VL+VBMI [AttachedDebugger]
  DefaultJob : .NET 8.0.23 (8.0.2325.60607), X64 RyuJIT AVX-512F+CD+BW+DQ+VL+VBMI
```
| Method                       | Mean     | Error    | StdDev   | Gen0   | Allocated |
|----------------------------- |---------:|---------:|---------:|-------:|----------:|
| &#39;Send (cold)&#39;                | 79.03 ns | 1.526 ns | 2.713 ns | 0.0257 |     432 B |
| &#39;Send (warm, same instance)&#39; | 79.21 ns | 1.566 ns | 3.783 ns | 0.0243 |     408 B |

## Contributing
Contributions are welcome! Please open an issue or pull request for bug fixes, improvements, or new features.

## License
This project is licensed under the Apache License.
