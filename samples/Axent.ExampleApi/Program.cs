using Axent.Abstractions;
using Axent.Core.DependencyInjection;
using Axent.ExampleApi;
using Axent.Extensions.AspNetCore;
using Axent.Extensions.FluentValidation;
using FluentValidation;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddValidatorsFromAssemblyContaining<ExampleCommandValidator>();

builder.Services.AddAxent(o => builder.Configuration.Bind("AppSettings:Axent", o))
    .AddHandlersFromAssemblyContaining<ExampleCommandHandler>()
    .AddTracing()
    .AddAutoFluentValidation()
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
        Message = "Hello Wooooooooooooooooooooooooooooooooooooooooooooooooooooooooooooooorld!"
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
