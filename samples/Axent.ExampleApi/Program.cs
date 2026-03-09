using Axent.Abstractions;
using Axent.Core.DependencyInjection;
using Axent.ExampleApi;
using Axent.Extensions.AspNetCore;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddAxent(o => builder.Configuration.Bind("AppSettings:Axent", o))
    .AddTracing()
    .AddRequestHandlersFromAssemblyContaining<ExampleRequestHandler>()
    .AddPipe<OtherRequestPipe>()
    .AddPipe(typeof(ExampleRequestPipe<,>));

var app = builder.Build();

app.MapGet("/", async (ISender sender, CancellationToken cancellationToken) =>
{
    var response = await sender.SendAsync(new WelcomeRequest(), cancellationToken);
    return response.ToResult();
});

app.MapGet("/api/example", async (ISender sender, CancellationToken cancellationToken) =>
{
    var request = new ExampleCommand
    {
        Message = "Hello World!"
    };

    var response = await sender.SendAsync(request, cancellationToken);
    return response.ToResult();
});

app.MapGet("/api/other", async (ISender sender, CancellationToken cancellationToken) =>
{
    var request = new OtherRequest
    {
        Message = "I'm Another Request"
    };

    var response = await sender.SendAsync(request, cancellationToken);
    return response.ToResult();
});

await app.RunAsync();
