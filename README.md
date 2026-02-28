# Axent

<svg width="620" height="160" viewBox="0 0 620 164" xmlns="http://www.w3.org/2000/svg" style="padding-bottom: 20px; background: white;">
  <g transform="translate(20,20)">
    <polygon points="60,0 120,35 120,105 60,140 0,105 0,35" fill="none" stroke="#000" stroke-width="6" stroke-linejoin="miter"></polygon>
    <polygon points="60,28 92,98 78,98 60,58 42,98 28,98" fill="#000"></polygon>
  </g>

  <text x="165" y="95" font-family="Segoe UI, Inter, Arial, sans-serif" font-size="64" font-weight="600" fill="#000" letter-spacing="1" dominant-baseline="middle">
    Axent
  </text>
</svg>

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

## Contributing
Contributions are welcome! Please open an issue or pull request for bug fixes, improvements, or new features.

## License
This project is licensed under the Apache License.
