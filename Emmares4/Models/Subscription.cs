using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Emmares4.Models
{
    public class Subscription
    {
        public int ID { get; set; }
        public ApplicationUser Subscriber { get; set; }
        public Provider Provider { get; set; }
        public ContentType ContentType { get; set; }
        public FieldOfInterest FieldOfInterest { get; set; }
        public Region Region { get; set; }
        public DateTime DateAdded { get; set; }
        public DateTime DateModified { get; set; }
        public int OptInStatus { get; set; }
    }
}
