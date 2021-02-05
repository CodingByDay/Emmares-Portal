using System;
using System.Collections.Generic;
using System.Linq;

using Nest;

namespace Emmares4.Elastic
{
    [ElasticsearchType(Name = "_doc")]
    public class StatisticsDocES
    {
        public string campaign_id { get; set; }
        public DateTime date_added { get; set; }
        public long rating { get; set; }
        public double reward { get; set; }
        public string user_Id { get; set; }
    }

    public class StatisticsES
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
                    settings.DefaultIndex("emmares_statistics");
                    client = new ElasticClient(settings);
                }
                return client;
            }
        }

        public static void UpdateDocument(StatisticsDocES doc)
        {
            GetClient().IndexDocument<StatisticsDocES>(doc);
        }

        public static void DeleteCampaignStats (string campaign_id)
        {
            GetClient().DeleteByQuery<StatisticsDocES>(s => s
                .Index("emmares_statistics")
                .Query(q => q.Term(t => t.campaign_id, campaign_id))
            );
        }
    }
}
