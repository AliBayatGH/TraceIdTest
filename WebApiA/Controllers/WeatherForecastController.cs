using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using System.Diagnostics;
using WebApiA.Services;

namespace WebApiA.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class WeatherForecastController : ControllerBase
    {
        private static readonly string[] Summaries = new[]
        {
        "Freezing", "Bracing", "Chilly", "Cool", "Mild", "Warm", "Balmy", "Hot", "Sweltering", "Scorching"
    };
        private readonly IMyService _myService;
        private readonly ILogger<WeatherForecastController> _logger;

        public WeatherForecastController(IMyService myService, ILogger<WeatherForecastController> logger)
        {
            _myService = myService;
            _logger = logger;
        }

        [HttpGet(Name = "GetWeatherForecast")]
        public async Task<IEnumerable<WeatherForecast>> Get()
        {
            var client = new HttpClient();
            var traceId = Activity.Current.TraceId;
            var spanId = Activity.Current.SpanId;
            var parentSpanId = Activity.Current.ParentSpanId;

            Activity.Current?.AddTag("WeatherForecast Id", "20021988");

            // _logger.LogInformation("TraceId:{TraceId}, SpanId:{SpanId}, ParentSpanId:{ParentSpanId}", traceId, spanId, parentSpanId);
            _logger.LogInformation("The GetWeatherForecast is calling!(WebApiA)");

            var result =await _myService.GetAsync();
            return Enumerable.Range(1, 5).Select(index => new WeatherForecast
            {
                Date = DateTime.Now.AddDays(index),
                TemperatureC = Random.Shared.Next(-20, 55),
                Summary = Summaries[Random.Shared.Next(Summaries.Length)]
            })
            .ToArray();
        }
    }
}