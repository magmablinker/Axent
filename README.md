# Axent
[![NuGet](https://img.shields.io/nuget/v/Axent.Abstractions?label=Axent.Abstractions)](https://www.nuget.org/packages/Axent.Abstractions)
[![NuGet](https://img.shields.io/nuget/v/Axent.Core?label=Axent.Core)](https://www.nuget.org/packages/Axent.Core)
[![NuGet](https://img.shields.io/nuget/v/Axent.Extensions.AspNetCore?label=Axent.Extensions.AspNetCore)](https://www.nuget.org/packages/Axent.Extensions.AspNetCore)
[![License](https://img.shields.io/badge/license-APACHE-blue)](LICENSE)

**Axent** is a lightweight, high-performance .NET library for implementing CQRS patterns with minimal boilerplate. It provides a simple request/response pipeline and allows adding custom processors for advanced scenarios.

---

## Features
- Minimal setup for CQRS in .NET applications
- Request/response handling with `RequestHandler<TRequest, TResponse>`
- Extensible pipelines using `IAxentPipe<TRequest, TResponse>`
- Optimized for performance and simplicity
- Works seamlessly with ASP.NET Core

---

## Prerequisites
- .NET 8 or later

## Getting started
### 1. Install Packages
```shell
dotnet add package Axent.Core --version 1.0.1
dotnet add package Axent.Extensions.AspNetCore --version 1.0.1
```

### 2. Register Services
```csharp
builder.Services.AddHttpContextAccessor()
builder.Services.AddAxent()
    .AddSender()
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

## Contributing
Contributions are welcome! Please open an issue or pull request for bug fixes, improvements, or new features.

## License 
This project is licensed under the Apache License.