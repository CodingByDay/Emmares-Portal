using System;
using System.Collections.Generic;

namespace Emmares4.Models
{
    public class Region
    {
        public int ID { get; set; }
        public string Code { get; set; }
        public string Name { get; set; }
        public DateTime DateAdded { get; set; }
        public DateTime DateModified { get; set; }
        public ICollection<Campaign> Compaigns { get; set; } = new List<Campaign>();
    }
}
