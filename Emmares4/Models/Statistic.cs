using System;

namespace Emmares4.Models
{
    public class Statistic
    {
        public int ID { get; set; }
        public int Rating { get; set; }
        public int Reward { get; set; }
        public DateTime DateAdded { get; set; }
        public DateTime DateModified { get; set; }
        public Campaign Campaign { get; set; }
        public ApplicationUser User { get; set; }
    }
}