using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Emmares4.Models
{
    public class Provider
    {
        public int ID { get; set; }
        public string Name { get; set; }
        public ApplicationUser Owner { get; set; }
        public string OwnerId { get; set; }
        public ICollection<Subscription> Subscribers { get; set; }
        public ICollection<Campaign> Campaigns { get; set; }
        public DateTime DateAdded { get; set; }
        public DateTime DateModified { get; set; }
    }
}
