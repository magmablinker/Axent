# Axent

![Axent Logo](https://raw.githubusercontent.com/magmablinker/Axent/refs/heads/main/logo/axent-logo.svg)

![GitHub Actions Workflow Status](https://img.shields.io/github/actions/workflow/status/magmablinker/axent/publish-nuget.yml)
[![NuGet](https://img.shields.io/nuget/v/Axent.Core)](https://www.nuget.org/packages/Axent.Core)
[![Downloads](https://img.shields.io/nuget/dt/Axent.Core.svg)](https://www.nuget.org/packages/Axent.Core/)
[![Quality Gate Status](https://sonarcloud.io/api/project_badges/measure?project=magmablinker_Axent&metric=alert_status)](https://sonarcloud.io/summary/new_code?id=magmablinker_Axent)

**Axent** is a source-generated CQRS library for modern .NET with typed pipelines and ASP.NET Core integration.

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

## 🚀 Getting Started

#### 1. Install Packages
```shell
dotnet add package Axent.Core
dotnet add package Axent.Extensions.AspNetCore
```

#### 2. Register Services
```csharp
builder.Services.AddAxent()
    .AddRequestHandlersFromAssemblyContaining<ExampleQueryHandler>();
```

#### 3. Create a Request and Handler
- IQuery<TResponse> for read operations
- ICommand<TResponse> for write operations
- IRequest<TResponse> if you don't want to differentiate
- IRequestHandler<TRequest, TResponse> to handle them

```csharp
using Axent.Abstractions.Models;
using Axent.Abstractions.Requests;
using Axent.Abstractions.Services;

namespace Axent.ExampleApi;

[Axent]
internal sealed record ExampleQuery(string Message) : IQuery<Unit>;

internal sealed class ExampleQueryHandler : IRequestHandler<ExampleQuery, Unit>
{
    private readonly ILogger<ExampleQueryHandler> _logger;

    public ExampleQueryHandler(ILogger<ExampleQueryHandler> logger)
    {
        _logger = logger;
    }

    public ValueTask<Response<Unit>> HandleAsync(ExampleQuery request, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Message from request '{Message}'", request.Message);
        return ValueTask.FromResult(Response.Success(Unit.Value));
    }
}
```

#### 4. Send a Request
Inject `IRequestSender<TRequest, TResponse>` into endpoints or application services for the generated typed fast path.

```csharp
app.MapGet("/api/example", async (IRequestSender<ExampleQuery, Unit> sender, CancellationToken cancellationToken) =>
{
    var response = await sender.SendAsync(new ExampleQuery("Hello World!"), cancellationToken);
    return response.ToResult();
});
```

`ISender` is still available for dynamic dispatch, but it is a compatibility adapter over generated typed senders and is not the recommended hot path.
---

Alternatively using the template
```shell
dotnet new install Axent.Templates
dotnet new axent-api
```

## 📖 Docs
To learn more about the features of Axent, checkout the [documentation](https://github.com/magmablinker/Axent/tree/main/docs)

## 📊 Benchmarks

### Axent (Source Generated Dispatch)
```
BenchmarkDotNet v0.14.0, Windows 11 (10.0.26200.8655)
Unknown processor
.NET SDK 11.0.100-preview.5.26302.115
  [Host]     : .NET 10.0.9 (10.0.926.27113), X64 RyuJIT AVX-512F+CD+BW+DQ+VL+VBMI [AttachedDebugger]
  DefaultJob : .NET 10.0.9 (10.0.926.27113), X64 RyuJIT AVX-512F+CD+BW+DQ+VL+VBMI
```
| Method                            | Mean     | Error    | StdDev   | Ratio | RatioSD | Gen0   | Allocated | Alloc Ratio |
|---------------------------------- |---------:|---------:|---------:|------:|--------:|-------:|----------:|------------:|
| &#39;SendAsync (cold)&#39;                | 16.46 ns | 0.524 ns | 1.536 ns |  1.01 |    0.13 | 0.0105 |     176 B |        1.00 |
| &#39;SendAsync (warm, same instance)&#39; | 14.10 ns | 0.266 ns | 0.337 ns |  0.86 |    0.08 | 0.0091 |     152 B |        0.86 |


### MediatR (v12.5.0)
```
BenchmarkDotNet v0.14.0, Windows 11 (10.0.26200.8655)
Unknown processor
.NET SDK 11.0.100-preview.5.26302.115
  [Host]     : .NET 10.0.9 (10.0.926.27113), X64 RyuJIT AVX-512F+CD+BW+DQ+VL+VBMI [AttachedDebugger]
  DefaultJob : .NET 10.0.9 (10.0.926.27113), X64 RyuJIT AVX-512F+CD+BW+DQ+VL+VBMI
```
| Method                       | Mean     | Error    | StdDev   | Gen0   | Allocated |
|----------------------------- |---------:|---------:|---------:|-------:|----------:|
| &#39;Send (cold)&#39;                | 49.26 ns | 0.941 ns | 1.813 ns | 0.0191 |     320 B |
| &#39;Send (warm, same instance)&#39; | 47.44 ns | 0.983 ns | 2.115 ns | 0.0176 |     296 B |


## 🤝 Contributing
Contributions are welcome.
If you find a bug, have an improvement, or want to propose a feature:
1. Open an issue
2. Start a discussion
3. Submit a pull request

## 📄 License
This project is licensed under the Apache License 2.0. See [`LICENSE`](https://github.com/magmablinker/Axent/blob/main/LICENSE) for details.
