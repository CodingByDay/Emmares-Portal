using System;
using System.Net;

namespace DeleteEmails
{
    class Program
    {
        static void Main(string[] args)
        {
            try
            {
                string deletesdate = DateTime.Today.ToString("yyyy-MM-dd");
                string jsonbody = "{\"query\": { \"match\": {\"enddate\": \"" + deletesdate + "\"}}}";
                WebClient wc4 = new WebClient();
                wc4.Headers.Add("Content-Type", "application/json");
                wc4.UploadString("http://172.17.1.88:9200/emmares_search_test/_delete_by_query", jsonbody);
                for (int i = 1; i < 8; i++)
                {
                    deletesdate = DateTime.Today.AddDays(-i).ToString("yyyy-MM-dd");
                    jsonbody = "{\"query\": { \"match\": {\"enddate\": \"" + deletesdate + "\"}}}";
                    wc4.Headers.Add("Content-Type", "application/json");
                    wc4.UploadString("http://172.17.1.88:9200/emmares_search_test/_delete_by_query", jsonbody);
                }
            }
            catch (Exception ex)
            {
                System.IO.File.AppendAllText("Delete_Mails_Log.txt", ex.ToString());
            }
        }
    }
}
