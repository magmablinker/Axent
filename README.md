# Axent

![Axent Logo](https://raw.githubusercontent.com/magmablinker/Axent/refs/heads/main/misc/axent-logo.svg)

[![NuGet](https://img.shields.io/nuget/v/Axent.Abstractions?label=Axent.Abstractions)](https://www.nuget.org/packages/Axent.Abstractions)
[![NuGet](https://img.shields.io/nuget/v/Axent.Core?label=Axent.Core)](https://www.nuget.org/packages/Axent.Core)
[![NuGet](https://img.shields.io/nuget/v/Axent.Extensions.AspNetCore?label=Axent.Extensions.AspNetCore)](https://www.nuget.org/packages/Axent.Extensions.AspNetCore)
[![NuGet](https://img.shields.io/nuget/v/Axent.Extensions.FluentValidation?label=Axent.Extensions.FluentValidation)](https://www.nuget.org/packages/Axent.Extensions.FluentValidation)
[![Downloads](https://img.shields.io/nuget/dt/Axent.Core.svg)](https://www.nuget.org/packages/Axent.Core/)
![GitHub Actions Workflow Status](https://img.shields.io/github/actions/workflow/status/magmablinker/Axent/pr.yml)
[![Quality Gate Status](https://sonarcloud.io/api/project_badges/measure?project=magmablinker_Axent&metric=alert_status)](https://sonarcloud.io/summary/new_code?id=magmablinker_Axent)
[![License](https://img.shields.io/badge/license-APACHE-blue)](LICENSE)

**Axent** is a lightweight, high-performance .NET library for implementing the CQRS pattern with minimal boilerplate. It provides a source-generated, typed request/response pipeline — currently ~2x faster than [MediatR (v12.5)](https://github.com/LuckyPennySoftware/MediatR) with fewer allocations.

---

## Why Axent?
* 🚀 Fast: source generated dispatch with zero reflection
* 🧩 Minimal: very little setup
* 🧠 Strongly typed, extensible pipelines for cross-cutting concerns
* 🌐 First class ASP.NET Core integration
* ⚙️ Built for modern .NET (8+)

## 📦 Features

- Minimal setup and boilerplate
- Source-generated dispatch — no reflection at runtime
- Typed pipelines with support for generic and request-specific pipes
- Separate marker interfaces for commands and queries (`ICommand<TResponse>`, `IQuery<TResponse>`)
- Built-in support for transactions, logging, and error handling via pipeline options
- ASP.NET Core integration
- .NET 8+ optimized

---

## Prerequisites

- .NET 8 or later

---

## 🚀 Getting Started

### Manual Installation

#### 1. Install Packages
```shell
dotnet add package Axent.Core --version 1.2.1
dotnet add package Axent.Extensions.AspNetCore --version 1.2.1
```

#### 2. Register Services
```csharp
builder.Services.AddAxent()
    .AddRequestHandlers(AssemblyProvider.Current);
```

#### 3. Create a Request and Handler
- IQuery<TResponse> for read operations
- ICommand<TResponse> for write operations
- IRequest<TResponse> if you don't want to differentiate
- IRequestHandler<TRequest, TResponse> to handle them

```csharp
using Axent.Abstractions;

namespace Axent.ExampleApi;

internal sealed class ExampleQuery : IQuery
{
    public required string Message { get; init; }
}

internal sealed class ExampleQueryHandler : IRequestHandler
{
    private readonly ILogger _logger;

    public ExampleQueryHandler(ILogger logger)
    {
        _logger = logger;
    }

    public ValueTask<Response> HandleAsync(RequestContext context, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Message from request '{Message}'", context.Request.Message);
        return ValueTask.FromResult(Response.Success(Unit.Value));
    }
}
```

#### 4. Send a Request
Inject ISender into endpoints or application services.

```csharp
app.MapGet("/api/example", async (ISender sender, CancellationToken cancellationToken) =>
{
    var response = await sender.SendAsync(new ExampleQuery { Message = "Hello World!" }, cancellationToken);
    return response.ToResult();
});
```
---

### Or alternatively using the template
```shell
dotnet new install Axent.Templates.MinimalApi
dotnet new axent-api
```

---

## 🔁 Pipelines
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

### 🌐 Generic Pipe
Runs for all request types.
```csharp
internal sealed class LoggingPipe : IAxentPipe
{
    private readonly ILogger<LoggingPipe> _logger;

    public LoggingPipe(ILogger<LoggingPipe> logger)
    {
        _logger = logger;
    }

    public ValueTask<Response> ProcessAsync(IPipelineChain chain, RequestContext context, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Handling {Request}", typeof(TRequest).Name);
        return chain.NextAsync(context, cancellationToken);
    }
}
```
#### Registration
```csharp
builder.Services.AddAxent()
    .AddRequestHandlers(AssemblyProvider.Current)
    .AddPipe(typeof(LoggingPipe));
```

### 🎯 Request Specific Pipe
Runs only for a single request type.
```csharp
internal sealed class OtherRequestPipe : IAxentPipe
{
    private readonly ILogger _logger;

    public OtherRequestPipe(ILogger logger)
    {
        _logger = logger;
    }

    public ValueTask<Response> ProcessAsync(IPipelineChain chain, RequestContext context, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Running pipe for OtherRequest");
        return chain.NextAsync(context, cancellationToken);
    }
}
```

#### Registration
```csharp
builder.Services.AddAxent()
    .AddRequestHandlers(AssemblyProvider.Current)
    .AddPipe();
```

---

## ⚙️ Configuration
Customize behavior via AxentOptions.
```csharp
builder.Services.AddAxent(options =>
{
    options.ErrorHandling = new AxentErrorHandlingOptions
    {
        EnableDetailedExceptionResponse = false
    };

    options.Logging = new AxentLoggingOptions
    {
        EnableRequestLogging = true
    };

    options.Transactions = new AxentTransactionOptions
    {
        UseTransactions = true
    };
});
```

| Option                                         | Description                                                                                                                                  | Default               |
|------------------------------------------------|----------------------------------------------------------------------------------------------------------------------------------------------|-----------------------|
| `ErrorHandling`                                | Enables pipeline exception handling. EnableDetailedExceptionResponse includes exception details in responses. If null, exceptions propagate. | `null`                |
| `Logging.EnableRequestLogging`                 | Logs the full request object via `ILogger` before processing. Avoid in production if requests contain sensitive data.                        | `false`               |
| `Transactions.UseTransactions`                 | Wraps `ICommand` handlers in a TransactionScope. Has no effect on `IQuery` handlers.                                                         | `true`                |
| `Transactions.TransactionOptions`              | Isolation level and timeout settings.                                                                                                        | `ReadCommitted`, 180s |
| `Transactions.TransactionScopeOption`          | Interaction with ambient transactions.                                                                                                       | `Required`            |
| `Transactions.TransactionScopeAsyncFlowOption` | Controls async transaction flow.                                                                                                             | `Enabled`             |

---

## FluentValidation Support
Axent can automatically validate requests by using [FluentValidation](https://github.com/FluentValidation/FluentValidation) in the pipeline.  
If a validator exists for a request, it is executed before the remaining pipes and the handler.

1. Install the packages
```shell
dotnet add package Axent.Extensions.FluentValidation --version 1.2.1
dotnet add package FluentValidation.DependencyInjectionExtensions --version 12.1.1
```
> `FluentValidation.DependencyInjectionExtensions` is optional. It is only needed if you want to register validators through assembly scanning.

2. Create a validator
```csharp
public sealed class ExampleCommandValidator : AbstractValidator<ExampleCommand>
{
    public ExampleCommandValidator()
    {
        RuleFor(r => r.Message)
            .NotEmpty()
            .MaximumLength(20);
    }
}
```
> Validators must be public when they are discovered via assembly scanning.

3. Register validators and enable FluentValidation
```csharp
builder.Services.AddValidatorsFromAssemblyContaining<ExampleCommandValidator>();

builder.Services.AddAxent()
    .AddHandlersFromAssemblyContaining<ExampleCommandHandler>()
    .AddAutoFluentValidation();
```

Once configured, Axent will automatically run the validator for the incoming request and stop pipeline execution early if validation fails.

---

## 📊 Benchmarks

### Axent (Source Generated Dispatch)
```
BenchmarkDotNet v0.14.0, Windows 11 (10.0.26200.7840)
Unknown processor
.NET SDK 10.0.200-preview.0.26103.119
  [Host]     : .NET 8.0.23 (8.0.2325.60607), X64 RyuJIT AVX-512F+CD+BW+DQ+VL+VBMI [AttachedDebugger]
  DefaultJob : .NET 8.0.23 (8.0.2325.60607), X64 RyuJIT AVX-512F+CD+BW+DQ+VL+VBMI
```
| Method                                    |     Mean |    Error |   StdDev | Ratio | RatioSD |   Gen0 | Allocated | Alloc Ratio |
|-------------------------------------------|---------:|---------:|---------:|------:|--------:|-------:|----------:|------------:|
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
| Method                               |     Mean |    Error |   StdDev |   Gen0 | Allocated |
|--------------------------------------|---------:|---------:|---------:|-------:|----------:|
| &#39;Send (cold)&#39;                | 79.03 ns | 1.526 ns | 2.713 ns | 0.0257 |     432 B |
| &#39;Send (warm, same instance)&#39; | 79.21 ns | 1.566 ns | 3.783 ns | 0.0243 |     408 B |

## 🤝 Contributing
Contributions are welcome.
If you find a bug, have an improvement, or want to propose a feature:
1. Open an issue
2. Start a discussion
3. Submit a pull request

## 📄 License
This project is licensed under the Apache License 2.0. See [`LICENSE`](https://github.com/magmablinker/Axent/blob/main/LICENSE) for details.
