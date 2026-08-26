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

        // Create logging sinks from configuration
        var sinks = LoggingPipelineFactory.CreateSinks(builder.Configuration);
        builder.Logging.AddConsole().SetMinimumLevel(LogLevel.Information);
        builder.Services.AddSingleton<ILoggerService>(sp =>
            new LoggingService(sp.GetRequiredService<ILogger<LoggingService>>(), sinks));

        // Register HttpContext accessor (required for WebCurrentUser)
        builder.Services.AddHttpContextAccessor();

        // Register current user identity from HTTP context
        builder.Services.AddScoped<ICurrentUser, WebCurrentUser>();

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

        app.UseStatusCodePages(async context =>
        {
            if (context.HttpContext.Response.StatusCode == StatusCodes.Status403Forbidden)
            {
                context.HttpContext.Response.Redirect("/AccessDenied");
            }

            await Task.CompletedTask;
        });

        app.UseAuthentication();
        app.UseAuthorization();

        app.MapRazorPages();

        app.Run();
    }
}