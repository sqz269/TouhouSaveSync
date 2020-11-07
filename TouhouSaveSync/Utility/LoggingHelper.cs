using NLog;
using NLog.Layouts;

namespace TouhouSaveSync.Utility
{
    public static class LoggingHelper
    {
        public static void ConfigureLogger()
        {
            var config = new NLog.Config.LoggingConfiguration();

            var logConsole = new NLog.Targets.ColoredConsoleTarget("logconsole")
            {
                Layout = Layout.FromString(
                    "${time} | <${logger:shortName=true}> [${level:uppercase=true}]: ${message} ${exception:format=tostring}")
            };

            var logFile = new NLog.Targets.FileTarget("logfile")
            {
                FileName = "log.log",
                Layout = Layout.FromString(
                    "${time} | <${logger}> [${level:uppercase=true}]: ${message} ${exception:format=tostring}")
            };

            config.AddRule(LogLevel.Trace, LogLevel.Fatal, logConsole);
            config.AddRule(LogLevel.Info, LogLevel.Fatal, logFile);

            NLog.LogManager.Configuration = config;
        }
    }
}