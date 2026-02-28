using Axent.Abstractions;
using Axent.Core;
using Axent.ExampleApi;
using Axent.Extensions.AspNetCore;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddAxent()
    .AddRequestHandlers(AssemblyProvider.Current)
    .AddPipe<OtherRequestPipe>()
    .AddPipe(typeof(ExampleRequestPipe<,>));

var app = builder.Build();

app.MapGet("/", () => "Axent Example Api is up and running!");

app.MapGet("/api/example", async (ISender sender, CancellationToken cancellationToken) =>
{
    var request = new ExampleRequest
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
