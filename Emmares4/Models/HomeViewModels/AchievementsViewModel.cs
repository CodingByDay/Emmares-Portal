using Emmares4.Data;
using Microsoft.AspNetCore.Identity;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;

namespace Emmares4.Models.HomeViewModels
{
    public class AchievementsViewModel
    {
        ApplicationDbContext _context;
        public AchievementsViewModel(ApplicationDbContext context, UserManager<ApplicationUser> userManager, ApplicationUser user)
        {
            _context = context;

            var sb = user.Balance.ToString("F");
            var i = sb.IndexOf('.');
            string sf = string.Empty, sp = string.Empty;
            if (i > 0)
            {
                sf = sb.Substring(0, sb.IndexOf('.'));
                sp = sb.Substring(sb.IndexOf('.') + 1);
                BalanceFullPart = int.Parse(sf);
                BalancePartialPart = int.Parse(sp);
                if (BalanceFullPart < 100)
                    Level = 1;
                else if (BalanceFullPart > 100 && BalanceFullPart < 200)
                    Level = 2;
                else if (BalanceFullPart > 200 && BalanceFullPart < 300)
                    Level = 3;
                else if (BalanceFullPart > 300 && BalanceFullPart < 400)
                    Level = 4;
                else if (BalanceFullPart > 500)
                    Level = 5;
            }

            RatedEmails = _context.Entry(user)
             .Collection(s => s.Statistics)
             .Query().Count();

            Activity = context.Entry(user)
             .Collection(s => s.Statistics)
             .Query()
             .Where(x => x.DateAdded > DateTime.UtcNow.AddDays(-30))
             .Count();
        }

        public int Activity { get; set; }
        public int BalanceFullPart { get; set; }
        public int BalancePartialPart { get; set; }
        public int RatedEmails { get; set; }
        public int Level { get; set; }
    }
}
