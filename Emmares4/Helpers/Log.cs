using System;
using System.IO;
using System.Text;
using System.Reflection;

namespace Emmares4.Helpers
{
    public class Log
    {
        private static object logLock = new object();
        private static string logPath = null;
        private static string logName = null;
        public static void Write (string info)
        {
            lock (logLock)
            {
                try
                {
                    if (logPath == null)
                    {
                        var path = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
                        logPath = Path.Combine(path, @"App_Data\Logs");
                        if (!Directory.Exists (logPath)) { Directory.CreateDirectory(logPath); }
                        logName = DateTime.Now.ToString("s").Replace(":", "-") + "-emmares.log";
                    }
                    using (var sw = new StreamWriter(Path.Combine(logPath, logName), true, Encoding.UTF8))
                    {
                        sw.WriteLine(DateTime.Now.ToString("s") + "\t" + info);
                    }
                }
                catch (Exception) { }
            }
        }
    }
}
