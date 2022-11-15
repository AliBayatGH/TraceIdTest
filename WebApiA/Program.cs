using Hasti.Framework.Endpoints.Logging.Extensions;
using Microsoft.Extensions.Configuration;
using WebApiA.Options;
using WebApiA.Services;

var builder = WebApplication.CreateBuilder(args);
builder.ConfigureLogging();

// Add services to the container.

builder.Services.AddControllers();
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.Configure<UrlsOptions>(builder.Configuration.GetSection("Urls"));
builder.Services.AddHttpClient<IMyService,MyService>();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();

app.Run();
