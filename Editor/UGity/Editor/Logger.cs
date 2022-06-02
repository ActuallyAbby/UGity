using System.Reflection;

namespace Octothorpe.UGity.Editor
{
    internal class Logger
    {
        public static void Log(string format, params object[] args) => LogInternal(LogLevel.Info, FormatMessage(Assembly.GetCallingAssembly(), format, args));

        public static void Log(LogLevel level, string format, params object[] args) => LogInternal(level, FormatMessage(Assembly.GetCallingAssembly(), format, args));

        private static void LogInternal(LogLevel level, string message)
        {
            if(level == LogLevel.Info)
                UnityEngine.Debug.Log(message);
            else if(level == LogLevel.Warning)
                UnityEngine.Debug.LogWarning(message);
            else if(level == LogLevel.Error)
                UnityEngine.Debug.LogError(message);
        }

        private static string FormatMessage(Assembly caller, string format, params object[] args)
        {
            string assemblyTitle = caller.GetCustomAttribute<AssemblyTitleAttribute>()?.Title ?? caller.GetName().Name;
            return string.Format(string.Concat("[", assemblyTitle, "] ", format), args);
        }
    }

    internal enum LogLevel
    {
        Info,
        Warning,
        Error,
    }
}
