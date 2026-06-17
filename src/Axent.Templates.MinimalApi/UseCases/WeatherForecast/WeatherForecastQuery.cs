using Axent.Abstractions.Attributes;
using Axent.Abstractions.Requests;
using Axent.Abstractions.Services;
using Axent.Abstractions.Models;

namespace Axent.Templates.MinimalApi.UseCases.Welcome;

[Axent]
internal sealed record WeatherForecastQuery : IQuery<WeatherForecast[]>;

internal sealed class WeatherForecastQueryHandler : IRequestHandler<WeatherForecastQuery, WeatherForecast[]>
{
    private static readonly string[] _summaries =
    [
        "Freezing", "Bracing", "Chilly", "Cool", "Mild", "Warm", "Balmy", "Hot", "Sweltering", "Scorching"
    ];

    public ValueTask<Response<WeatherForecast[]>> HandleAsync(WeatherForecastQuery request,
        CancellationToken cancellationToken = default)
    {
        var rng = new Random();
        return ValueTask.FromResult(Response.Success(Enumerable.Range(1, 5).Select(index => new WeatherForecast
        {
            Date = DateTime.Now.AddDays(index),
            TemperatureC = rng.Next(-20, 55),
            Summary = _summaries[rng.Next(_summaries.Length)]
        })
        .ToArray()));
    }
}
