using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using TestIA.Logging;

namespace TestIA.Logging;

/// <summary>
/// Factory that reads the <c>Logging.Sinks</c> section from <see cref="IConfiguration"/>
/// and creates the corresponding <see cref="ILoggingSink"/> instances.
/// </summary>
/// <remarks>
/// Supported sink types:
/// <list type="bullet">
///   <item><description>File — writes to a text file (requires <c>Logging.Sinks:File:Path</c>)</description></item>
///   <item><description>Console — writes to standard output (no additional configuration required)</description></item>
/// </list>
/// Additional sinks can be added by implementing <see cref="ILoggingSink"/> and extending this factory.
/// </remarks>
public static class LoggingPipelineFactory
{
    /// <summary>
    /// Creates <see cref="ILoggingSink"/> instances from the <c>Logging.Sinks</c> configuration section.
    /// </summary>
    /// <param name="configuration">The configuration containing the <c>Logging.Sinks</c> section.</param>
    /// <returns>A collection of configured <see cref="ILoggingSink"/> instances.</returns>
    public static IEnumerable<ILoggingSink> CreateSinks(IConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(configuration);

        var sinks = new List<ILoggingSink>();
        var sinksSection = configuration.GetSection("Logging:Sinks");

        if (!sinksSection.Exists())
        {
            return sinks;
        }

        // File sink
        var fileConfig = sinksSection.GetSection("File");
        if (fileConfig.Exists())
        {
            var path = fileConfig.GetValue<string>("Path");
            if (!string.IsNullOrEmpty(path))
            {
                sinks.Add(new FileLoggingSink(path));
            }
        }

        // Console sink
        var consoleConfig = sinksSection.GetSection("Console");
        if (consoleConfig.Exists())
        {
            sinks.Add(new ConsoleLoggingSink());
        }

        return sinks;
    }
}
