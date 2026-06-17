using Axent.Abstractions.Services;
using Axent.Core.DependencyInjection;
using Axent.Example.Application;
using Axent.ExampleApi;
using Axent.Extensions.AspNetCore;
using Axent.Extensions.Caching;
using Axent.Extensions.FluentValidation;
using FluentValidation;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddValidatorsFromAssemblyContaining<ExampleCommandValidator>();

var apiAssembly = typeof(OtherQuery).Assembly;
var applicationAssembly = typeof(ExampleCommand).Assembly;

builder.Services.AddAxent(o => builder.Configuration.Bind("AppSettings:Axent", o), assemblies: [apiAssembly, applicationAssembly])
    .AddRequestHandlersFromAssembly(apiAssembly)
    .AddRequestHandlersFromAssembly(applicationAssembly)
    .AddTracing()
    .AddAutoFluentValidation()
    .AddCache()
    .AddPipe<OtherQueryPipe>()
    .AddPipe(typeof(ExampleRequestPipe<,>));

var app = builder.Build();

app.MapGet("/", async (IRequestSender<WelcomeRequest, string> sender, CancellationToken cancellationToken) =>
{
    var response = await sender.SendAsync(new WelcomeRequest(), cancellationToken);
    return response.ToResult();
});

app.MapGet("/api/example", async (IRequestSender<ExampleCommand, ExampleResponse> sender, CancellationToken cancellationToken) =>
{
    var request = new ExampleCommand
    {
        Message = "Hello Wooooooooooooooooooooooooooooooooooooooooooooooooooooooooooooooorld!"
    };

    var response = await sender.SendAsync(request, cancellationToken);
    return response.ToResult();
});

app.MapGet("/api/other", async (IRequestSender<OtherQuery, OtherResponse> sender, CancellationToken cancellationToken) =>
{
    var request = new OtherQuery
    {
        Message = "I'm Another Request"
    };

    var response = await sender.SendAsync(request, cancellationToken);
    return response.ToResult();
});

await app.RunAsync();
