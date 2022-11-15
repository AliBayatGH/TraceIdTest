using Serilog;
using Microsoft.AspNetCore.Builder;

namespace Hasti.Framework.Endpoints.Logging.Extensions;

public static class WebApplicationBuilderExtensions
{
    public static WebApplicationBuilder ConfigureLogging(this WebApplicationBuilder builder)
    {
        builder.Host.UseSerilog((ctx, sp, lc) => lc.WithDefaultConfiguration(ctx,sp));

        return builder;
    }
}