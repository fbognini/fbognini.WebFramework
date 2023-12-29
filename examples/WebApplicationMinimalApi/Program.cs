using fbognini.WebFramework.Handlers;
using fbognini.WebFramework.Behaviours;
using WebApplicationMinimalApi.Handlers.WeatherForecasts.GetWeaterForecast;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services.AddMediatR(cfg =>
{
    cfg.RegisterServicesFromAssemblyContaining<Program>();
    cfg.AddIHttpRequestValidationBehavior();
});
builder.Services.AddMediatRExceptionHandler();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

var summaries = new[]
{
    "Freezing", "Bracing", "Chilly", "Cool", "Mild", "Warm", "Balmy", "Hot", "Sweltering", "Scorching"
};

app.MediateGet<GetWeatherForecastQuery>("/weatherforecast")
    .WithName("GetWeatherForecast")
    .WithOpenApi();

app.Run();
