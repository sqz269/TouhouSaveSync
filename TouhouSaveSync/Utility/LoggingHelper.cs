using NLog;
using NLog.Layouts;

namespace TouhouSaveSync.Utility
{
    public static class LoggingHelper
    {
        public static void ConfigureLogger()
        {
            var config = new NLog.Config.LoggingConfiguration();

            var logConsole = new NLog.Targets.ColoredConsoleTarget("logconsole");
            logConsole.Layout = Layout.FromString("${time} | <${logger:shortName=true}> [${level:uppercase=true}]: ${message} ${exception:format=tostring}");

            config.AddRule(LogLevel.Trace, LogLevel.Fatal, logConsole);
            
            NLog.LogManager.Configuration = config;
        }
    }
}