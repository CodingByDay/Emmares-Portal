using Emmares4.Data;
using Microsoft.AspNetCore.Identity;
using System.Collections.Generic;
using System.Data.Common;

using Emmares4.Helpers;

namespace Emmares4.Models.HomeViewModels
{
    public class MyContentViewModel
    {
        ApplicationDbContext _context;
        DbConnection _dbConnection;
        public MyContentViewModel(ApplicationDbContext context, UserManager<ApplicationUser> userManager, ApplicationUser user, DbConnection dbConnection)
        {
            _dbConnection = dbConnection;
            _context = context;

            _context.Entry(user)
                .Collection(x => x.Statistics)
                .Load();

            FavoritesVM = new FavoritesViewModel(context, userManager, user);
            SubscriptionsVM = new MySubscriptionViewModel(context, userManager, user, _dbConnection);

            OpenLinks = new List<string>();
            while (true)
            {
                var url = LinksBag.Get(user.Id);
                if (!string.IsNullOrWhiteSpace(url))
                {
                    OpenLinks.Add(url);
                }
                else
                {
                    break;
                }
            }
        }

        public FavoritesViewModel FavoritesVM { get; set; }
        public MySubscriptionViewModel SubscriptionsVM { get; set; }

        public List<string> OpenLinks { get; set; }
        
        public string[] keyWordList { get; set; }
        
        public string keyword { get; set; }
        public List<int> providersByKeyWords { get; set; }
    }
}
