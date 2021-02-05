using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Emmares4.Helpers;
using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations; 
namespace Emmares4.Models
{
    public class KeyWords
    {

        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity) ]
        public int ID { get; set; }
        public string KeyWord { get; set; }
        //public ICollection<CampaignsHasKeywords> CampaignsHasKeywords { get; } = new List<CampaignsHasKeywords>();
        public List<CampaignsHasKeywords> campaignsset { get; set; }
    }
}
