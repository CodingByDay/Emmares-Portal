using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Web;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using System.IO;
using System.Reflection;
using Emmares4.Helpers;
using System.Globalization;

// For more information on enabling Web API for empty projects, visit https://go.microsoft.com/fwlink/?LinkID=397860

namespace Emmares4
{

    public class teststrings
    {

        public string ena { get; set; }
        public string dve { get; set; }
        public decimal tri { get; set; }

    }





    [Route("[controller]/[action]")]
    public class SearchAPI : Controller
    {
        const string elastichost = "http://172.17.1.88:9200";

        // GET: api/<controller>
        [HttpGet]
        /* public IEnumerable<string> Get()
         {
             // return new string[] { "value1", "value2" };
             return "did not match any documents";
         }*/

        public string Get()
        {
            return "Search did not find any results.";
        }



        // POST /log/map

        // {

        // "keyword": "Zeus",
  
        // "time":"2015-01-01",
  
        // "location":"Olymp"

        // }


        // GET api/<controller>/5
        [HttpGet("{id}")]
        // [Route("api/Test/{id}")]
        public string Get(string id)
        {

            /* HttpClient client = new HttpClient();
            client.BaseAddress = new Uri("elastichost + /emmares_search_test/_search?q=test");
            client.DefaultRequestHeaders.Accept.Clear();
            client.DefaultRequestHeaders.Accept.Add(
            new MediaTypeWithQualityHeaderValue("application/json")); */

            WebClient wc = new WebClient();
            try
            {
                return wc.DownloadString(elastichost + "/emmares_search_test/_search?q=" + HttpUtility.UrlEncode(id));
                
            } //+ "&filter_path=hits.hits._source"
            catch { return "Do not use \", (, ), : and other special characters"; }

            /*
            async Task<teststrings> GetProductAsync(string path)
            {
                teststrings teststr1 = null;
                HttpResponseMessage response = await client.GetAsync(path);
                if (response.IsSuccessStatusCode)
                {
                    teststr1 = await response.Content.ReadAsAsync<teststrings>();
                }
                return teststr1;
            }*/


            //return "value";
        }
        [HttpGet("{id}/{page}")]
        public string get_search(string id, string page)

        {
            
            string[] separated = page.Split(' ');
            var pagewithextension = separated[0];
            var pagewithoutequals = pagewithextension.Split("=")[1];
            if(pagewithoutequals == "page")
            {
                pagewithoutequals = "0";
            } else
            {
                pagewithoutequals = pagewithoutequals;
            }
            /* WebClient wc = new WebClient(); // to dela
            try
            {
                return wc.DownloadString(elastichost + "/emmares_search_test/_search?q=" + HttpUtility.UrlEncode(id) + "&filter_path=hits");
            } 
            catch { return "Do not use \", (, ), : and other special characters"; }/* // to dela */

            //var httpWebRequest = (HttpWebRequest)WebRequest.Create(elastichost + "/emmares_search_test/_search");
            //httpWebRequest.ContentType = "application/json";
            // httpWebRequest.Method = "POST";

            // using (var streamWriter = new StreamWriter(httpWebRequest.GetRequestStream()))
            // {
    //        {

    //            "query" : {
    //                "bool" : {
    //                    "must" : {
    //                        "query_string": {
    //                            "query": "grcija"
    //                        }
    //                    },
    //            "should" : [
    //               {
    //                        "range" : {
    //                            "date" : {
    //                                "boost" : 5,
    //          "gte" : "2021-07-01"
    //                            }
    //                        }
    //                    },
    //           {
    //                        "range" : {
    //                            "date" : {
    //                                "boost" : 4,
    //          "gte" : "2021-05-01"
    //                            }
    //                        }
    //                    },
    //           {
    //                        "range" : {
    //                            "date" : {
    //                                "boost" : 3,
    //          "gte" : "2021-03-01"
    //                            }
    //                        }
    //                    }
    //  ]
    //}
    //            }
    //        }

            string json = "{\"query\": {\"multi_match\" : {\"query\" : \"" + id + "\",\"fuzziness\": \"AUTO\"}}}";
            string jsonPriority = "{\"query\": {\"bool\" : {\"must\" : {\"query_string\" : {\"query\"  :\"" + id + "\",\"should\": [\"range\" : {\"date\" : {\"boost\" :\"" + 4 + "\", \"gte\" :\"" + "2021-03-01\"}}}";
            string jsonfields = "{\"query\": {\"multi_match\" : {\"query\" : \"" + id + "\",\"fields\": [\"excerpt\", \"preview\"],\"fuzziness\": \"AUTO\"}}}";
           // "{\"query\": {\"multi_match\" : {\"query\" : \"" + id + "\",\"fuzziness\": \"AUTO\"}}}";
            //streamWriter.Write(json);
            // streamWriter.Flush();
            // streamWriter.Close();

            WebClient wc = new WebClient();
            wc.Headers.Add("Content-Type", "application/json");
            try
            {
                var test = Log(id);

                return wc.UploadString(elastichost + $"/emmares_search_test/_search?size=10&&from={pagewithoutequals}", jsonfields); // fixed size of the return...
            }
            catch
            {
                return "Error";
            }
            //}
           

            /* var httpResponse = (HttpWebResponse)httpWebRequest.GetResponse(); 
             using (var streamReader = new StreamReader(httpResponse.GetResponseStream()))
             {
                 var result = streamReader.ReadToEnd();
                 return result;
             }*/
            
        }




        [HttpGet("{id}/{page}")]
        public string get_search_page(string id, string page)
        {
           
            string json = "{\"query\": {\"multi_match\" : {\"query\" : \"" + id + "\",\"fuzziness\": \"AUTO\"}}}";
            string jsonPriority = "{\"query\": {\"bool\" : {\"must\" : {\"query_string\" : {\"query\"  :\"" + id + "\",\"should\": [\"range\" : {\"date\" : {\"boost\" :\"" + 4 + "\", \"gte\" :\"" + "2021-03-01\"}}}";
            string jsonfields = "{\"query\": {\"multi_match\" : {\"query\" : \"" + id + "\",\"fields\": [\"excerpt\", \"preview\"],\"fuzziness\": \"AUTO\"}}}";
            // "{\"query\": {\"multi_match\" : {\"query\" : \"" + id + "\",\"fuzziness\": \"AUTO\"}}}";
            //streamWriter.Write(json);
            // streamWriter.Flush();
            // streamWriter.Close();

            WebClient wc = new WebClient();
            wc.Headers.Add("Content-Type", "application/json");
            try
            {
                var test = Log(id);

                return wc.UploadString(elastichost + $"/emmares_search_test/_search?size=10&&from={page}", jsonfields); // fixed size of the return...
            }
            catch
            {
                return "Error";
            }
            //}


            /* var httpResponse = (HttpWebResponse)httpWebRequest.GetResponse(); 
             using (var streamReader = new StreamReader(httpResponse.GetResponseStream()))
             {
                 var result = streamReader.ReadToEnd();
                 return result;
             }*/

        }







        /// <summary>
        /// Logs: GeoLocation, keyword, and date of the event.
        /// </summary>
        /// <param name="keyword"></param>
        /// <returns></returns>
        private bool Log(string keyword)
        {

            // Keyword contains the keyword and the ID, using static helper class.

            var search_value = ApiHelper.GetUntilOrEmpty(keyword, " ");
            var key = keyword;
            string country = Request.Cookies["country"];
            // string search_value = Request.Cookies["searchString"];
            // GetGeoLocation()
            // For now Slovenia.
            DateTime dateTime = DateTime.UtcNow.Date;
            var justDate = dateTime.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);
            string jsonbody = "{ \"time\" : \"" + justDate + "\", \"location\" : \"" + country + "\", \"keyword\" : \"" + search_value + "\" }";
            // Here is a problem. Actually what happens is that searched value cookie does not change for some reason.
            WebClient wc = new WebClient();
            wc.Headers.Add("Content-Type", "application/json");
            try
            {
                var debug = wc.UploadString(elastichost + "/log/map", jsonbody); // fixed size of the return...[address/index/mapping/id]
                return true;
            }
            catch
            {
                return false;
            }
        }

        /// 






        // POST api/<controller>
        [HttpPost]
        public void Post([FromBody]string value)
        {
        }

        // PUT api/<controller>/5
        [HttpPut("{id}")]
        public void Put(int id, [FromBody]string value)
        {
        }

        // DELETE api/<controller>/5
        [HttpDelete("{id}")]
        public void Delete(int id)
        {
        }

        [HttpGet("{id}")]
        public ContentResult Get_File(string id)
        {
            var path = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
            var logPath = Path.Combine(path, @"App_Data\pages\");
            string html_page = System.IO.File.ReadAllText(logPath + id + ".html");

            return new ContentResult
            {
                ContentType = "text/html; charset=utf-8",
                StatusCode = (int)HttpStatusCode.OK,
                Content = html_page
            };

        }

        [HttpGet]//("{CampaignID}, {Sender_Email}")]
        public string SaveToDB(string CampaignID, string Sender_Email)
        {
            DBWriter.Write("insert into Maping_Campaign_Sender (CampaignID, Sender_Email) values (@CampaignID, @Sender_Email)", (p) =>
            {
                p.Add(DBParameter.String("CampaignID", CampaignID));
                p.Add(DBParameter.String("Sender_Email", Sender_Email));

            });

            return "OK";

        }

        

    }
}
