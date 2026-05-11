using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;
using MTM_Waitlist_Server.Api.Services;
using MTM_Waitlist_Server.Core.Interfaces.Dashboard;
using MTM_Waitlist_Server.Core.Models.Dashboard;
using System.Diagnostics;

namespace MTM_Waitlist_Server.Api;

/// <summary>
/// Configures and builds the ASP.NET Core web application that hosts the Kestrel REST API.
/// Called by the WinUI Admin host in App.xaml.cs — this is not a standalone entry point.
/// </summary>
public static class ApiStartup
{
    /// <summary>
    /// Creates a configured <see cref="WebApplication"/> bound to the supplied DI services.
    /// The caller is responsible for calling <see cref="WebApplication.RunAsync"/> on a
    /// background thread so it does not block the WinUI message loop.
    /// </summary>
    public static WebApplication BuildApp(string listenUrl, IServiceCollection sharedServices)
    {
        var builder = WebApplication.CreateBuilder();

        // Re-register all shared services so the ASP.NET DI container shares the same singletons.
        foreach (var descriptor in sharedServices)
        {
            builder.Services.Add(descriptor);
        }

        builder.Services.AddControllers();
        // TODO: configure JWT bearer auth once FEATURE-01 is implemented.
        builder.Services.AddAuthentication();
        builder.Services.AddAuthorization();

        builder.WebHost.UseUrls(listenUrl);

        var app = builder.Build();

        app.UseAuthentication();
        app.UseAuthorization();

        // Request-logging middleware — appends every request/response to the in-process ring buffer.
        app.Use(async (ctx, next) =>
        {
            var sw = Stopwatch.StartNew();
            await next();
            sw.Stop();

            var buffer = ctx.RequestServices.GetService<IActivityLogBuffer>();
            buffer?.Append(new LogEntry(
                DateTime.UtcNow,
                ctx.Request.Method,
                ctx.Request.Path,
                ctx.Response.StatusCode,
                sw.ElapsedMilliseconds));
        });

        app.MapControllers();

        return app;
    }
}
