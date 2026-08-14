using Microsoft.Extensions.Logging;
using TestIA;
using TestIA.Application;
using TestIA.Domain;
using TestIA.Logging;

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

        app.MapRazorPages();

        app.Run();
    }
}