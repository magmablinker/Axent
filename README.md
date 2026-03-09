# Axent

![Axent Logo](https://raw.githubusercontent.com/magmablinker/Axent/refs/heads/main/logo/axent-logo.svg)

![GitHub Actions Workflow Status](https://img.shields.io/github/actions/workflow/status/magmablinker/axent/publish-nuget.yml)
[![NuGet](https://img.shields.io/nuget/v/Axent.Core)](https://www.nuget.org/packages/Axent.Core)
[![Downloads](https://img.shields.io/nuget/dt/Axent.Core.svg)](https://www.nuget.org/packages/Axent.Core/)
[![Quality Gate Status](https://sonarcloud.io/api/project_badges/measure?project=magmablinker_Axent&metric=alert_status)](https://sonarcloud.io/summary/new_code?id=magmablinker_Axent)

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

## 🚀 Getting Started

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
