using System;
using Microsoft.AspNetCore;
using Microsoft.AspNetCore.Hosting;

using Emmares4.Helpers;

namespace Emmares4
{
    public class Program
    {
        public static void Main(string[] args)
        {
            try
            {
                Log.Write("Emmares.Main start");
                var wh = BuildWebHost(args);
                Log.Write("Emmares.Main web host ready");
                wh.Run();
            }
            catch (Exception ex)
            {
                Log.Write("Emmares.Main web host failed to run: " + ex.ToString ());
            }
        }

        public static IWebHost BuildWebHost(string[] args) =>
            WebHost.CreateDefaultBuilder(args)
                .UseApplicationInsights()
                .UseIISIntegration()
                .UseStartup<Startup>()
                .Build();
    }
}
