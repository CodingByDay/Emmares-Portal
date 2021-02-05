using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Identity;

namespace Emmares4.Models
{
    // Add profile data for application users by adding properties to the ApplicationUser class
    public class ApplicationUser : IdentityUser
    {
        public string FirstName { get; set; }
        public string LastName { get; set; }
        public string WalletAddress { get; set; }
        public double Balance { get; set; }
        public Provider Publisher { get; set; }

        public ICollection<Statistic> Statistics { get; set; }
        public ICollection<UserInterest> Interests { get; set; }
        public ICollection<Subscription> Subscriptions { get; set; }
        public DateTime DateAdded { get; set; } = DateTime.Now;

        public DateTime DateModified { get; set; }
    }
}
