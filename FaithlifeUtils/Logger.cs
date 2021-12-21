using System;
using System.IO;
using System.Reflection;
using Serilog;
using Serilog.Events;

namespace FaithlifeUtils;

/// <summary>
/// A wrapper class for <see cref="Log"/> for configuration and resource management.
/// </summary>
public sealed class Logger : IDisposable
{
    /// <summary>
    /// Configures <see cref="Log"/>'s logging
    /// </summary>
    public Logger()
    {
        var template = "[{Timestamp:HH:mm:ss} {Level:u3} {SourceContext}] {Message:lj}\n{Exception}";
        var logPath = $"{Assembly.GetExecutingAssembly().GetName().Name}.log";
        File.Delete(logPath);
        Log.Logger = new LoggerConfiguration()
            .MinimumLevel.Debug()
            .WriteTo.Console(LogEventLevel.Information, template)
            .WriteTo.File(logPath, outputTemplate: template)
            .CreateLogger();
        Log.Logger.ForContext<Logger>().Debug("Logger configured");
    }

    /// <summary>
    /// Closes and flushes the logger in a safe manner
    /// </summary>
    public void Dispose()
    {
        Log.Logger.ForContext<Logger>().Debug("Logger disposing");
        Log.CloseAndFlush();
        GC.SuppressFinalize(this);
    }
}
