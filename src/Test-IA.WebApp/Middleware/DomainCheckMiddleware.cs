using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using System.Runtime.Versioning;

namespace TestIA.WebApp.Middleware;

[SupportedOSPlatform("windows")]
public class DomainCheckMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<DomainCheckMiddleware> _logger;

    public DomainCheckMiddleware(RequestDelegate next, ILogger<DomainCheckMiddleware> logger)
    {
        _next = next ?? throw new ArgumentNullException(nameof(next));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task InvokeAsync(HttpContext context)
    {
        var path = context.Request.Path.Value ?? string.Empty;
        if (path.StartsWith("/css/", StringComparison.OrdinalIgnoreCase) ||
            path.StartsWith("/js/", StringComparison.OrdinalIgnoreCase) ||
            path.StartsWith("/images/", StringComparison.OrdinalIgnoreCase) ||
            path.StartsWith("/favicon.ico", StringComparison.OrdinalIgnoreCase) ||
            path.StartsWith("/Error", StringComparison.OrdinalIgnoreCase))
        {
            await _next(context);
            return;
        }

        var isDomainJoined = IsDomainJoined();

        if (!isDomainJoined)
        {
            _logger.LogWarning("Machine is not joined to an Active Directory domain. Active Directory features are not available. Request path: {Path}", path);
            context.Response.StatusCode = StatusCodes.Status503ServiceUnavailable;
            context.Response.ContentType = "text/html; charset=utf-8";
            await context.Response.WriteAsync(BuildDomainNotAvailableHtml(path));
            return;
        }

        _logger.LogDebug("Machine is joined to an Active Directory domain. Proceeding with request: {Path}", path);
        await _next(context);
    }

    private static bool IsDomainJoined()
    {
        try
        {
            var domain = System.DirectoryServices.ActiveDirectory.Domain.GetCurrentDomain();
            return domain != null;
        }
        catch
        {
            return false;
        }
    }

    private static string BuildDomainNotAvailableHtml(string currentPath)
    {
        var backLink = string.IsNullOrEmpty(currentPath) || currentPath == "/" ? "/Users" : currentPath;
        return BuildHtml(backLink);
    }

    private static string BuildHtml(string backLink)
    {
        var sb = new System.Text.StringBuilder();
        sb.AppendLine("<!DOCTYPE html><html lang=\"en\"><head><meta charset=\"utf-8\" /><title>Active Directory Not Available</title>");
        sb.Append("<style>body{font-family:Segoe UI,sans-serif;background:#f5f5f5;color:#333;display:flex;justify-content:center;align-items:center;min-height:100vh;margin:0;padding:20px;}.error-container{background:#fff;border-radius:8px;box-shadow:0 2px 10px rgba(0,0,0,0.1);padding:40px;max-width:600px;text-align:center;}.error-icon{font-size:48px;color:#d9534f;margin-bottom:20px;}h1{color:#d9534f;font-size:24px;margin-bottom:16px;}p{font-size:16px;line-height:1.6;color:#555;margin-bottom:12px;}.info-box{background:#f8f9fa;border-left:4px solid #0275d8;padding:16px;margin:20px 0;text-align:left;border-radius:4px;}.info-box strong{color:#0275d8;}a{display:inline-block;margin-top:20px;padding:10px 24px;background:#0275d8;color:#fff;text-decoration:none;border-radius:4px;font-size:14px;}a:hover{background:#025aa5;}</style>");
        sb.AppendLine("</head><body><div class=\"error-container\"><div class=\"error-icon\">&#9888;</div><h1>Active Directory Not Available</h1>");
        sb.Append("<p>This machine is <strong>not joined</strong> to an Active Directory domain. Active Directory features (user and group lookup) are not available.</p>");
        sb.Append("<div class=\"info-box\"><strong>To resolve this issue:</strong><ul style=\"text-align:left;margin-top:8px\"><li>Join this computer to the Active Directory domain.</li><li>Ensure the machine has network access to a Domain Controller.</li><li>Restart the application after joining the domain.</li></ul></div>");
        sb.Append("<a href=\"").Append(backLink).Append("\">&#8592; Return to Application</a></div></body></html>");
        return sb.ToString();
    }
}
