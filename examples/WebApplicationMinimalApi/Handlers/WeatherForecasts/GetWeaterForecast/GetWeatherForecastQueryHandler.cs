using fbognini.Core.Exceptions;
using MediatR;

namespace WebApplicationMinimalApi.Handlers.WeatherForecasts.GetWeaterForecast;

public class GetWeatherForecastQueryHandler: IRequestHandler<GetWeatherForecastQuery, IResult>
{
    private readonly ILogger<GetWeatherForecastQuery> logger;

    public GetWeatherForecastQueryHandler(
        ILogger<GetWeatherForecastQuery> logger)
    {
        this.logger = logger;
    }

    public async Task<IResult> Handle(GetWeatherForecastQuery query, CancellationToken cancellationToken)
    {
        //throw new BadRequestException();
        //throw new NotFoundException(typeof(WeatherForecast), 52);

        var summaries = new[]
        {
            "Freezing", "Bracing", "Chilly", "Cool", "Mild", "Warm", "Balmy", "Hot", "Sweltering", "Scorching"
        };


        var forecast = Enumerable.Range(1, 5).Select(index =>
            new WeatherForecast
            (
                DateOnly.FromDateTime(DateTime.Now.AddDays(index)),
                Random.Shared.Next(-20, 55),
                summaries[Random.Shared.Next(summaries.Length)]
            ))
            .ToArray();

        return Results.Ok(forecast);
    }
}
