using Axent.Abstractions.Services;
using Axent.Core.DependencyInjection;
using Axent.Extensions.AspNetCore;
using Axent.Templates.MinimalApi.UseCases.WeatherForecast;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddAxent(o => builder.Configuration.Bind("Axent", o))
    .AddTracing()
    .AddHandlersFromAssemblyContaining<WeatherForecastQueryHandler>();

var app = builder.Build();

app.MapGet("/", () => "API is up and running");

app.MapGet("/api/weather-forecast", async (IRequestSender<WeatherForecastQuery, WeatherForecast[]> sender, CancellationToken cancellationToken) =>
{
    var response = await sender.SendAsync(new WeatherForecastQuery(), cancellationToken);
    return response.ToResult();
});

await app.RunAsync();
