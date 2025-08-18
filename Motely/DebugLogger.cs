using System;

namespace Motely
{
    public static class DebugLogger
    {
        public static bool IsEnabled { get; set; }

        public static void Log(string message)
        {
            if (IsEnabled)
            {
                Console.WriteLine($"[DEBUG] {message}");
            }
        }

        public static void LogFormat(string format, params object[] args)
        {
            if (IsEnabled)
            {
                Console.WriteLine($"[DEBUG] {string.Format(format, args)}");
            }
        }
    }
}