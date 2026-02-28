# Axent

![Logo](https://raw.githubusercontent.com/magmablinker/Axent/refs/heads/feature/source-generator/misc/axent-logo.svg)

[![NuGet](https://img.shields.io/nuget/v/Axent.Abstractions?label=Axent.Abstractions)](https://www.nuget.org/packages/Axent.Abstractions)
[![NuGet](https://img.shields.io/nuget/v/Axent.Core?label=Axent.Core)](https://www.nuget.org/packages/Axent.Core)
[![NuGet](https://img.shields.io/nuget/v/Axent.Extensions.AspNetCore?label=Axent.Extensions.AspNetCore)](https://www.nuget.org/packages/Axent.Extensions.AspNetCore)
[![Downloads](https://img.shields.io/nuget/dt/Axent.Core.svg)](https://www.nuget.org/packages/Axent.Core/)
[![License](https://img.shields.io/badge/license-APACHE-blue)](LICENSE)

**Axent** is a lightweight, high-performance .NET library for implementing CQRS patterns with minimal boilerplate. It provides a simple request/response pipeline and allows adding custom processors for advanced scenarios.

---

## Features
- Minimal setup for CQRS in .NET applications
- Request/response handling with `RequestHandler<TRequest, TResponse>`
- Extensible pipelines using `IAxentPipe<TRequest, TResponse>`
- Optimized for performance and simplicity
- Works seamlessly with ASP.NET Core
- Choose between a source generated or reflection based sender implementation

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

    public override Task<Response<Unit>> HandleAsync(RequestContext<ExampleRequest> context, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Message from request '{0}'", context.Request.Message);
        return Task.FromResult(Response.Success(Unit.Value));
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

    public ValueTask<Response<TResponse>> ProcessAsync(Func<ValueTask<Response<TResponse>>> next, RequestContext<TRequest> context, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("This pipe runs during every request.");
        return next();
    }
}

builder.Services.AddAxent()
    .AddRequestHandlers(AssemblyProvider.Current)
    .AddPipe(typeof(ExampleRequestPipe<,>));
```
> This pipe executes for every request handled by Axent.

### Specific Pipe
```csharp
internal sealed class OtherRequestPipe : IAxentPipe<OtherRequest, Unit>
{
    private readonly ILogger<OtherRequestPipe> _logger;

    public OtherRequestPipe(ILogger<OtherRequestPipe> logger)
    {
        _logger = logger;
    }


    public ValueTask<Response<Unit>> ProcessAsync(Func<ValueTask<Response<Unit>>> next, RequestContext<OtherRequest> context, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("I only run during other request");
        return next();
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
    /// Determines whether to use the source-generated sender implementation.
    /// Defaults to true.
    /// </summary>
    public bool UseSourceGeneratedSender { get; set; } = true;
}
```

### Using Reflection Based Sender Implementation

You can switch to the reflection-based sender by setting UseSourceGeneratedSender to false:

```csharp
builder.Services.AddAxent(o => o.UseSourceGeneratedSender = false)
    .AddRequestHandlers(AssemblyProvider.Current)
    .AddPipe<OtherRequestPipe>();
```
> By default, if no options are provided, the source-generated sender is used.

## Benchmark

### Source Generated Dispatch
```
BenchmarkDotNet v0.14.0, Windows 11 (10.0.26200.7840)
11th Gen Intel Core i9-11900K 3.50GHz, 1 CPU, 16 logical and 8 physical cores
.NET SDK 9.0.203
  [Host]     : .NET 8.0.1 (8.0.123.58001), X64 RyuJIT AVX-512F+CD+BW+DQ+VL+VBMI
  DefaultJob : .NET 8.0.1 (8.0.123.58001), X64 RyuJIT AVX-512F+CD+BW+DQ+VL+VBMI
```

| Method                            | Mean     | Error    | StdDev   | Median   | Ratio | RatioSD | Gen0   | Allocated | Alloc Ratio |
|---------------------------------- |---------:|---------:|---------:|---------:|------:|--------:|-------:|----------:|------------:|
| &#39;SendAsync (cold)&#39;                | 267.4 ns | 20.59 ns | 60.72 ns | 229.1 ns |  1.05 |    0.32 | 0.0753 |     632 B |        1.00 |
| &#39;SendAsync (warm, same instance)&#39; | 220.6 ns |  4.37 ns |  6.12 ns | 219.7 ns |  0.86 |    0.17 | 0.0725 |     608 B |        0.96 |

### Reflection Dispatch
```
BenchmarkDotNet v0.14.0, Windows 11 (10.0.26200.7840)
11th Gen Intel Core i9-11900K 3.50GHz, 1 CPU, 16 logical and 8 physical cores
.NET SDK 9.0.203
  [Host]     : .NET 8.0.1 (8.0.123.58001), X64 RyuJIT AVX-512F+CD+BW+DQ+VL+VBMI
  DefaultJob : .NET 8.0.1 (8.0.123.58001), X64 RyuJIT AVX-512F+CD+BW+DQ+VL+VBMI
```

| Method                            | Mean       | Error    | StdDev    | Median     | Ratio | RatioSD | Gen0   | Allocated | Alloc Ratio |
|---------------------------------- |-----------:|---------:|----------:|-----------:|------:|--------:|-------:|----------:|------------:|
| &#39;SendAsync (cold)&#39;                | 1,149.5 ns | 56.12 ns | 165.46 ns | 1,205.2 ns |  1.02 |    0.22 | 0.1249 |   1.02 KB |        1.00 |
| &#39;SendAsync (warm, same instance)&#39; |   853.3 ns | 16.94 ns |  25.35 ns |   852.8 ns |  0.76 |    0.13 | 0.1221 |      1 KB |        0.98 |


## Contributing
Contributions are welcome! Please open an issue or pull request for bug fixes, improvements, or new features.

## License
This project is licensed under the Apache License.
