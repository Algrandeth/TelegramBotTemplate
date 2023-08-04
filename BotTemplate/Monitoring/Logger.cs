﻿namespace Template.Monitoring
{
    public static class Logger
    {
        private static string logPath = "logs.txt";


        public static async Task StartMessage(string appName)
        {
            if (File.Exists(logPath))
                Console.WriteLine(await File.ReadAllTextAsync(logPath));

            await LogMessage("Started at " + appName);
        }


        public static async Task LogMessage(string text)
        {
            try
            {
                Console.ForegroundColor = ConsoleColor.DarkCyan;
                var logText = $"| {DateTime.UtcNow.AddHours(3):HH:mm:ss yyyy-MM-dd} |  INFO    |  " + text;

                Console.WriteLine(logText);
                Console.ResetColor();
                await WriteLog(logText);
            }
            catch
            { }
        }



        public static async Task LogError(string text)
        {
            try
            {
                Console.ForegroundColor = ConsoleColor.DarkYellow;
                var logText = $"| {DateTime.UtcNow.AddHours(3):HH:mm:ss yyyy-MM-dd} |  ERROR   |  " + text;

                Console.WriteLine(logText);
                Console.ResetColor();
                await WriteLog(logText);
            }
            catch
            { }
        }


        public static async Task LogCritical(string text)
        {
            try
            {
                Console.ForegroundColor = ConsoleColor.DarkRed;
                var logText = $"| {DateTime.UtcNow.AddHours(3):HH:mm:ss yyyy-MM-dd} | CRITICAL |  " + text;

                Console.WriteLine(logText);
                Console.ResetColor();
                await WriteLog(logText);
            }
            catch
            { }
        }


        private static async Task WriteLog(string text)
        {
            try
            {
                await Task.Run(() =>
                {
                    string prevLogs = "";
                    if (File.Exists(logPath)) prevLogs = File.ReadAllText(logPath);

                    prevLogs = prevLogs += $"{text}\n";
                    File.WriteAllText(logPath, prevLogs);
                });
            }
            catch (Exception) { }
        }
    }
}

