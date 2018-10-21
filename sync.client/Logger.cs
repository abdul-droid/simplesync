using sync.core;
using System;
using System.IO;

namespace sync.client
{
    internal static class Logger
    {
        private static string LineBreak => string.Format("{1}{0}{1}", new String('\t', 25), Environment.NewLine);

        public static void LogErrorToTextFile(this Exception ErrorLog)
        {
            using (StreamWriter writer = new StreamWriter(AppConfig.LogErrorTextFileName.GetTexFilePath(), true))
            {
                writer.WriteLine($"Message: {ErrorLog.Message}{Environment.NewLine} StackTrace: { ErrorLog.StackTrace}{Environment.NewLine} Date:{ DateTime.Now.ToString()} { LineBreak}");
            }
        }

        public static void LogToTextFile(this string InfoLog)
        {
            using (StreamWriter writer = new StreamWriter(AppConfig.LogInfoTextFileName.GetTexFilePath(), true))
            {
                writer.WriteLine($"Info: {InfoLog}{Environment.NewLine} Date:{ DateTime.Now.ToString()} {LineBreak}");
            }
        }

        private static string GetTexFilePath(this string FileName)
        {
            return AppConfig.FilePath.MakeTextFilePath(FileName);
        }
    }
}
