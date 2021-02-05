using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Emmares4.Helpers;
namespace Emmares4.Models
{
    public class SubscriptionsLog
    {

        public int ID { get; set; }
        public DateTime DateModified { get; set; }
        public int OptIn { get; set; }
        public string SubscriberID { get; set; }
        public string ProviderID { get; set; }
        public string CampaignID { get; set; }


    }
}
