using System;
using System.IO;

namespace sync.client
{
    public static class AppConfig
    {
        internal static string ClientBaseAddressUri;
        internal static string RequestUri;
        internal static int TimerInterval;

        public static void Configure()
        {
            string appConfigFile = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData), @"Pulse Software\Unity POS\unity_config.xml");
            //string appConfigFile = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData), "pulse.xml");


        }

        public static void LogErrorToTextFile(Exception ex)
        {
            string filePath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData), "pulse_sync_error.txt"); ;

            using (StreamWriter writer = new StreamWriter(filePath, true))
            {
                writer.WriteLine("Message:" + ex.Message + "<br/>" + Environment.NewLine + "StackTrace:" + ex.StackTrace +
                   "" + Environment.NewLine + "Date:" + DateTime.Now.ToString());
                writer.WriteLine(Environment.NewLine + "-----------------------------------------------------------------------------" + Environment.NewLine);
            }
        }

        public static void LogToTextFile(string s)
        {
            string filePath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData), "pulse_sync_info.txt"); ;

            using (StreamWriter writer = new StreamWriter(filePath, true))
            {
                writer.WriteLine("Info:" + s + "<br/>" + Environment.NewLine +
                   "" + Environment.NewLine + "Date :" + DateTime.Now.ToString());
                writer.WriteLine(Environment.NewLine + "-----------------------------------------------------------------------------" + Environment.NewLine);
            }
        }
    }
}

