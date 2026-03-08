using Axent.Abstractions;

namespace Axent.Templates.MinimalApi.UseCases.Welcome;

internal sealed record WeatherForecastQuery : IQuery<WeatherForecast[]>;

internal sealed class WelcomeQueryHandler : IRequestHandler<WeatherForecastQuery, WeatherForecast[]>
{
    private static readonly string[] _summaries =
    [
        "Freezing", "Bracing", "Chilly", "Cool", "Mild", "Warm", "Balmy", "Hot", "Sweltering", "Scorching"
    ];

    public ValueTask<Response<WeatherForecast[]>> HandleAsync(RequestContext<WeatherForecastQuery> context,
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
