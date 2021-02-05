using System.Security.Claims;
using Emmares4.Data;
using Emmares4.Models;
using Microsoft.AspNetCore.Identity;

namespace Emmares4.Models.HomeViewModels
{
    public class WithdrawlViewModel
    {
        private ApplicationDbContext _db;
        private UserManager<ApplicationUser> _userManager;
        
        public WithdrawlViewModel(ApplicationDbContext db, UserManager<ApplicationUser> userManager, ApplicationUser user)
        {
            _db = db;
            _userManager = userManager;
            Address = user.WalletAddress;
            Balance = user.Balance;
        }

        public string Address { get; set; }
        public double Balance { get; set; }
    }
}