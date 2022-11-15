using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Serilog;
using Serilog.Enrichers.AspnetcoreHttpcontext;
using Serilog.Enrichers.Span;
using Serilog.Events;
using Serilog.Exceptions;
using Serilog.Exceptions.Core;
using Serilog.Exceptions.EntityFrameworkCore.Destructurers;
using Serilog.Filters;
using Serilog.Formatting.Json;
using Serilog.Sinks.Elasticsearch;
using Serilog.Sinks.File;
using System.Reflection;
using System.Security.Claims;

namespace Hasti.Framework.Endpoints.Logging.Extensions;
public static class LoggerConfigurationExtensions
{
    public static LoggerConfiguration WithDefaultConfiguration(this LoggerConfiguration loggerConfig, HostBuilderContext hostBuilderContext,IServiceProvider serviceProvider)
    {
        IConfiguration configuration = hostBuilderContext.Configuration;
        string? elasticsearchUri = configuration["ConnectionStrings:ElasticConnectionString"];

        string? assemblyName = Assembly.GetEntryAssembly()?.GetName().Name;

        IHostEnvironment hostEnvironment = hostBuilderContext.HostingEnvironment;
        loggerConfig
        .Enrich.FromLogContext()
        .Enrich.WithEnvironmentName()
            .Enrich.WithProperty("ApplicationName", hostEnvironment.ApplicationName)
            .Enrich.WithProperty("EnvironmentName", hostEnvironment.EnvironmentName)
            .MinimumLevel.Override("Microsoft", LogEventLevel.Error)
            .MinimumLevel.Override("System.Net.Http.HttpClient", LogEventLevel.Warning)
            .MinimumLevel.Override("Microsoft.Hosting.Lifetime", LogEventLevel.Information)
            .MinimumLevel.Override("System", LogEventLevel.Error)
            .MinimumLevel.Override("DotNetCore.CAP", LogEventLevel.Error)
            .MinimumLevel.Override("HIT", LogEventLevel.Debug)
             .Enrich.With<ActivityEnricher>()
            .Enrich.WithMachineName()
            .Enrich.WithProperty("Assembly", assemblyName);

        if (serviceProvider is not null)
        {
            //loggerConfig.Enrich.WithAspnetcoreHttpcontext(serviceProvider, GetContextInfo);
        }

        loggerConfig.Enrich.WithExceptionDetails(new DestructuringOptionsBuilder()
            .WithDefaultDestructurers()
            .WithDestructurers(new[] { new DbUpdateExceptionDestructurer() }));

        loggerConfig.WriteTo.Console(outputTemplate: "[{Timestamp:HH:mm:ss} {Level}] {SourceContext}{NewLine}{Message:lj}{NewLine}{Exception}{NewLine}");

        if (!string.IsNullOrWhiteSpace(elasticsearchUri))
        {
            loggerConfig.WriteTo.Logger(lc =>
                        lc.Filter.ByExcluding(Matching.WithProperty<bool>("Security", p => p))
                            .WriteTo.Elasticsearch(new ElasticsearchSinkOptions(new Uri(elasticsearchUri))
                            {
                                AutoRegisterTemplate = true,
                                AutoRegisterTemplateVersion = AutoRegisterTemplateVersion.ESv7,
                                IndexFormat = $"{assemblyName!.ToLower().Replace(".", "-")}-{hostEnvironment.EnvironmentName?.ToLower().Replace(".", "-")}-{DateTime.UtcNow:yyyy-MM}",
                                //IndexFormat = "logs-{0:yyyy.MM.dd}",
                                BatchAction = ElasticOpType.Create,
                                TypeName = null,
                                InlineFields = true,
                                FailureCallback = e => Console.WriteLine("Unable to submit event " + e.MessageTemplate),
                                EmitEventFailure = EmitEventFailureHandling.WriteToSelfLog |
                                       EmitEventFailureHandling.WriteToFailureSink |
                                       EmitEventFailureHandling.RaiseCallback
                            }))
                    .WriteTo.Logger(lc =>
                        lc.Filter.ByIncludingOnly(Matching.WithProperty<bool>("Security", p => p))
                            .WriteTo.Elasticsearch(new ElasticsearchSinkOptions(new Uri(elasticsearchUri))
                            {
                                AutoRegisterTemplate = true,
                                AutoRegisterTemplateVersion = AutoRegisterTemplateVersion.ESv7,
                                IndexFormat = "security-{0:yyyy.MM.dd}",
                                BatchAction = ElasticOpType.Create,
                                InlineFields = true
                            }));
        }

        loggerConfig.ReadFrom.Configuration(configuration); // minimum levels defined per project in json files 

        return loggerConfig;
    }

    private static ContextInformation? GetContextInfo(IHttpContextAccessor httpContextAccessor)
    {
        HttpContext? httpContext = httpContextAccessor.HttpContext;
        if (httpContext == null)
        {
            return null;
        }

        return new ContextInformation
        {
            Path = httpContext.Request.Path.ToString(),
            RemoteIpAddress = httpContext.Connection.RemoteIpAddress.ToString(),
            Host = httpContext.Request.Host.ToString(),
            Method = httpContext.Request.Method,
            Protocol = httpContext.Request.Protocol,
            UserInfo = GetUserInfo(httpContext.User),
        };
    }

    private static UserInformation GetUserInfo(ClaimsPrincipal claimsPrincipal)
    {
        System.Security.Principal.IIdentity user = claimsPrincipal.Identity;
        if (user?.IsAuthenticated != true)
        {
            return null;
        }

        List<string> excludedClaims = new()
            {
                "nbf",
                "exp",
                "auth_time",
                "amr",
                "sub",
                "at_hash",
                "s_hash",
                "sid",
                "name",
                "preferred_username"
            };

        const string userIdClaimType = "sub";
        const string userNameClaimType = "name";

        UserInformation userInformation = new()
        {
            UserId = claimsPrincipal.Claims.FirstOrDefault(a => a.Type == userIdClaimType)?.Value,
            Username = claimsPrincipal.Claims.FirstOrDefault(a => a.Type == userNameClaimType)?.Value,
            UserClaims = new Dictionary<string, List<string>>()
        };

        foreach (string distinctClaimType in claimsPrincipal.Claims
            .Where(claim => excludedClaims.All(ex => ex != claim.Type))
            .Select(claim => claim.Type)
            .Distinct())
        {
            userInformation.UserClaims[distinctClaimType] = claimsPrincipal.Claims
                .Where(a => a.Type == distinctClaimType)
                .Select(c => c.Value)
                .ToList();
        }

        return userInformation;
    }
}

public class ContextInformation
{
    public string Path { get; set; }
    public string Host { get; set; }
    public string Method { get; set; }
    public string RemoteIpAddress { get; set; }
    public string Protocol { get; set; }
    public UserInformation UserInfo { get; set; }
}

public class UserInformation
{
    public string UserId { get; set; }
    public string Username { get; set; }
    public Dictionary<string, List<string>> UserClaims { get; set; }
}
