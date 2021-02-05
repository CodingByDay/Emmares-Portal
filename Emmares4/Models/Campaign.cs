using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

using Emmares4.Helpers;

namespace Emmares4.Models
{
    public class Campaign
    {
        public Guid ID { get; set; }
        [Required]
        public string Name { get; set; }
        [Required]
        public int Recipients { get; set; }
        public double Budget { get; set; }
        public string Snippet { get { return SnippetGenerator.Get(this); } }
        public DateTime DateAdded { get; set; }
        public DateTime DateModified { get; set; }
        public Provider Publisher { get; set; }
        public int FieldOfInterestID { get; set; }
        public FieldOfInterest FieldOfInterest { get; set; }
        public int ContentTypeID { get; set; }
        public ContentType ContentType { get; set; }
        public int RegionID { get; set; }
        public Region Region { get; set; }
        public ICollection<Statistic> Statistics { get; set; }

        [NotMapped]
        public double AvailableBalance { get; set; }

        public string OptInLink { get; set; }
        public string OptOutLink { get; set; }
        public string AffiliateLink { get; set; }
        public string CampaignDescription { get; set; }
        public List<CampaignsHasKeywords> keyWordsset { get; set;  }
        

        [NotMapped]
        public string inputKeyWords { get; set; }
        [NotMapped]
        public string inputKeyWordsID { get; set; }




    }

}
//[Bind("ID,Name,Publisher,Recipients,Budget,Snippet,ContentTypeID,FieldOfInterestID,RegionID")] 