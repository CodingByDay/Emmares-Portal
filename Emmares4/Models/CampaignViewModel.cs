using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;

namespace Emmares4.Models
{
    public class CampaignViewModel
    {
        public Guid ID { get; set; }
        public string Name { get; set; }
        public string FieldOfInterest { get; set; }
        public string Region { get; set; }
        public string ContentType { get; set; }
        public int Recipients { get; set; }
        public double Budget { get; set; }
        public double Remaining { get; set; }
        public DateTime AddedOn { get; set; }
        public int Hits { get; set; }
    }
}
