using Axent.Abstractions;
using Axent.Core.DependencyInjection;
using Axent.Extensions.AspNetCore;
using Axent.Templates.MinimalApi;
using Axent.Templates.MinimalApi.UseCases.Welcome;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddAxent(o => builder.Configuration.Bind("Axent", o))
    .AddTracing()
    .AddRequestHandlersFromAssembly(AssemblyProvider.Current);

var app = builder.Build();

app.MapGet("/", () => "API is up and running");

app.MapGet("/api/weather-forecast", async (ISender sender, CancellationToken cancellationToken) =>
{
    var response = await sender.SendAsync(new WeatherForecastQuery(), cancellationToken);
    return response.ToResult();
});

await app.RunAsync();
