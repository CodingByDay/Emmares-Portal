using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Emmares4.Helpers;
using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;
namespace Emmares4.Models
{
    public class CampaignsHasKeywords
    {

        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int ID { get; set; }
        public Campaign campaignset{ get; set; }
        public KeyWords keyWordsset { get; set; }
        public Guid CampaignID { get; set; }
        public int KeyWordID { get; set; }

    
    }
}
