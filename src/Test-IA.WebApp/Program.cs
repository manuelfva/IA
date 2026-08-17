using Microsoft.AspNetCore.Authentication.Negotiate;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using TestIA;
using TestIA.Application;
using TestIA.Domain;
using TestIA.Logging;
using TestIA.WebApp;

/// <summary>
/// Main entry point for the Test-IA Web Application.
/// Configures the ASP.NET Core pipeline, registers services via dependency injection,
/// and starts the Kestrel web server to serve the Razor Pages interface.
/// </summary>
public class Program
{
    /// <summary>
    /// Builds and runs the web application.
    /// </summary>
    /// <param name="args">Command-line arguments.</param>
    public static void Main(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);

        // Configure logging
        builder.Logging.AddConsole().SetMinimumLevel(LogLevel.Information);

        // Register logging service
        builder.Services.AddSingleton<ILoggerService>(sp =>
            new LoggingService(sp.GetRequiredService<ILogger<LoggingService>>()));

        // Register Test-IA AD services
        builder.Services.AddTestIAServices();

        // Register configuration-based services
        builder.Services.Configure<AuthorizationSettings>(builder.Configuration.GetSection("Authorization"));

        // Register Windows Authentication
        builder.Services.AddAuthentication(NegotiateDefaults.AuthenticationScheme)
            .AddNegotiate();

        // Register authorization with policy
        builder.Services.AddAuthorization(options =>
        {
            var requiredGroup = builder.Configuration.GetValue<string>("Authorization:RequiredGroup") ?? string.Empty;
            options.AddPolicy("RequiredGroup", policy =>
                policy.RequireAuthenticatedUser()
                      .AddRequirements(new GroupAuthorizationRequirement(requiredGroup)));
        });

        builder.Services.AddSingleton<IAuthorizationHandler, GroupAuthorizationHandler>();

        // Register Razor Pages
        builder.Services.AddRazorPages();

        var app = builder.Build();

        if (!app.Environment.IsDevelopment())
        {
            app.UseHttpsRedirection();
            app.UseExceptionHandler("/Error");
            app.UseHsts();
        }

        app.UseStaticFiles();
        app.UseRouting();
        app.UseAuthentication();
        app.UseAuthorization();

        app.MapRazorPages();

        app.Run();
    }
}