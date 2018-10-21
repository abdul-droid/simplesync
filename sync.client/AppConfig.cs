using System;
using System.Configuration;

namespace sync.client
{
    public static class AppConfig
    {
        internal static string SqlServerConnectionString = string.Empty;
        internal static string WebApiUrl = string.Empty;
        internal static string LogErrorTextFileName = string.Empty;
        internal static string LogInfoTextFileName = string.Empty;
        internal static string FilePath = string.Empty;
        internal static int TimerInterval = 0;

        public static void Configure()
        {
            SqlServerConnectionString = ConfigurationManager.ConnectionStrings["SqlServerConnectionString"].ConnectionString;
            WebApiUrl = ConfigurationManager.AppSettings["WebApiUrl"].ToString();
            LogErrorTextFileName = ConfigurationManager.AppSettings["LogErrorTextFileName"].ToString();
            LogInfoTextFileName = ConfigurationManager.AppSettings["LogInfoTextFileName"].ToString();
            FilePath = Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData);
            if (Int32.TryParse(ConfigurationManager.AppSettings["TimerInterval"].ToString(), out TimerInterval)) { TimerInterval = 0; }
        }
    }
}

