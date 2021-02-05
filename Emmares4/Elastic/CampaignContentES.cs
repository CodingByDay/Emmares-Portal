using System;
using System.Collections.Generic;
using System.Linq;

using Nest;

namespace Emmares4.Elastic
{
    [ElasticsearchType(Name = "_doc")]
    public class CampaignDocES
    {
        public string campaign_id { get; set; }
        public string content { get; set; }
    }

    public class CampaignContentES
    {
        private static ElasticClient client = null;
        private static object syncLock = new object();
        private static ElasticClient GetClient()
        {
            lock (syncLock)
            {
                if (client == null)
                {
                    var node = new Uri("http://172.17.1.88:9200");
                    var settings = new ConnectionSettings(node);
                    settings.DefaultIndex("emmares_campaigns");
                    client = new ElasticClient(settings);
                }
                return client;
            }
        }

        public static void UpdateDocument(string id, string content)
        {
            var d = new CampaignDocES { campaign_id = id, content = content };
            GetClient().IndexDocument<CampaignDocES>(d);
        }

        public static List<string> ListDocuments(string terms)
        {
            var results = GetClient().Search<CampaignDocES>(s => s
                .StoredFields(sf => sf
                    .Fields(
                        f => f.campaign_id
                    )
                )
                .Index("emmares_campaigns") 
                .From(0)
                .Size(2000)
                .Scroll("5m")
                .Query(q => q.Terms(t => t.Field (f => f.content).Terms (terms.Split (' '))))
            );
            var hits = results.Hits;
            return hits.OrderByDescending (r => r.Score).Select(r => r.Source.campaign_id.ToLower ()).ToList();
        }
    }
}
