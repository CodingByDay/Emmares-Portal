using Emmares4.Data;
using Microsoft.AspNetCore.Identity;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;

using Emmares4.Helpers;

namespace Emmares4.Models.HomeViewModels
{
    public class FavoritesViewModel
    {
        ApplicationDbContext _context;
        public FavoritesViewModel(ApplicationDbContext context, UserManager<ApplicationUser> userManager, ApplicationUser user)
        {
            _context = context;

            var interests = _context.Entry(user)
               .Collection(s => s.Interests)
               .Query()
               .Select(p => p.FieldOfInterest.Name)
               .ToList();

            Favorites = interests;

            var lastFav = SetFavoriteDictionary.Get(user.Id);
            LastSelected = lastFav != null ? lastFav : (interests.Count > 0 ? interests [0] : "");
        }

        public List<string> Favorites { get; set; }

        public string LastSelected { get; set; }

        public int FieldOfInterestID { get; set; }
    }
}
