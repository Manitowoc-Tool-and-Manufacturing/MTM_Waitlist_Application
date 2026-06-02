using System.Diagnostics;
using System.Text;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Tokens;
using MTM_Waitlist_Server.Api.Data;
using MTM_Waitlist_Server.Api.Services;
using MTM_Waitlist_Server.Core.Interfaces.Dashboard;
using MTM_Waitlist_Server.Core.Interfaces.Settings;
using MTM_Waitlist_Server.Core.Models.Dashboard;

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
    public static WebApplication BuildApp(
        string listenUrl,
        IServiceCollection sharedServices,
        IServiceProvider sharedProvider)
    {
        ArgumentNullException.ThrowIfNull(sharedProvider);

        var builder = WebApplication.CreateBuilder();

        // Re-register all shared service types, then replace singleton services with
        // factories that resolve from the live WinUI provider. This keeps in-memory
        // services, such as the kill switch heartbeat store, shared by the API and UI.
        foreach (var descriptor in sharedServices)
        {
            if (descriptor.Lifetime == ServiceLifetime.Singleton)
            {
                builder.Services.Replace(ServiceDescriptor.Singleton(
                    descriptor.ServiceType,
                    _ => sharedProvider.GetRequiredService(descriptor.ServiceType)));
                continue;
            }

            builder.Services.Add(descriptor);
        }

        builder.Services.AddSingleton<Dao_InforVisualWorkOrder>();
        builder.Services.AddSingleton<Service_ApiAuth>();
        builder.Services.AddSingleton<Service_ApiSetupTech>();
        builder.Services.AddSingleton<Service_ApiWaitlist>();

        // Explicitly register this assembly as an application part so that MVC
        // discovers the controllers defined here even when the WebApplication host
        // is launched from the WinUI Admin process (a different assembly).
        builder.Services.AddControllers()
            .AddApplicationPart(typeof(ApiStartup).Assembly);

        // Resolve the JWT secret from shared settings so tokens issued by Service_ApiAuth
        // can be validated by the same key.
        var settingsStore = sharedProvider.GetRequiredService<IService_SettingsStore>();
        var jwtSecret = settingsStore.Get().Api.JwtSecret;
        var keyBytes = jwtSecret.Length > 0
            ? Encoding.UTF8.GetBytes(jwtSecret)
            : Encoding.UTF8.GetBytes("MTM-Waitlist-Development-Secret-Key-32-chars"); // Fallback for first-run without settings

        builder.Services
            .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
            .AddJwtBearer(options =>
            {
                options.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuer = false,
                    ValidateAudience = false,
                    ValidateLifetime = true,
                    ValidateIssuerSigningKey = true,
                    IssuerSigningKey = new SymmetricSecurityKey(keyBytes),
                };
            });
        builder.Services.AddAuthorization();

        builder.WebHost.UseUrls(listenUrl);

        var app = builder.Build();

        app.UseAuthentication();
        app.UseAuthorization();

        app.MapMethods("/health", new[] { "GET", "HEAD" }, () => Results.Ok());

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
