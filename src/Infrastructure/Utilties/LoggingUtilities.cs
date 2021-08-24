using Microsoft.Extensions.Logging;

namespace DN.WebApi.Infrastructure.Utilties
{
    public static class LoggingUtilities
    {
        // LoggerFactory is the name of system class; So, It's not a good idea to use it again.
        public static ILoggerFactory MyLoggerFactory { get; set; } = LoggerFactory.Create(builder =>
        {
            // Add Console Logger and config to show log level information
            builder.AddConsole(configure: config => { builder.SetMinimumLevel(LogLevel.Information); });
        });

        public static ILogger CreateLogger<T>() => MyLoggerFactory.CreateLogger<T>();
        public static ILogger CreateLogger(string categoryName) => MyLoggerFactory.CreateLogger(categoryName);
    }
}