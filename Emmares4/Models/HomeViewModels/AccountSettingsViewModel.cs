using Emmares4.Data;
using Emmares4.Models.ManageViewModels;
using Microsoft.AspNetCore.Identity;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;

namespace Emmares4.Models.HomeViewModels
{
    public class AccountSettingsViewModel
    {
        private ApplicationDbContext _context;
        private UserManager<ApplicationUser> _userManager;
        private ClaimsPrincipal user;
        public string UserImage { get; set; }

        public AccountSettingsViewModel()
        {

        }

        public AccountSettingsViewModel(ApplicationDbContext context, UserManager<ApplicationUser> userManager, ApplicationUser user)
        {

            _context = context;
            _userManager = userManager;            
            _context = context;
            
            var pub = _context.Providers.Where(x => x.Owner == user).FirstOrDefault();

            FirstName = user.FirstName;
            LastName = user.LastName;
            Email = user.Email;
            WalletAddress = user.WalletAddress;
            PublisherName = pub?.Name;
            UserImage = user.Id + ".jpg";
        }

        public string FirstName { get; set; }
        public string LastName { get; set; }
        public string Email { get; set; }
        public string WalletAddress { get; set; }

        //[Required]
        [DataType(DataType.Password)]
        [Display(Name = "Current password")]
        public string OldPassword { get; set; }

        //[Required]
        //[StringLength(100, ErrorMessage = "The {0} must be at least {2} and at max {1} characters long.", MinimumLength = 6)]
        [DataType(DataType.Password)]
        [Display(Name = "New password")]
        public string NewPassword { get; set; }

        [DataType(DataType.Password)]
        [Display(Name = "Confirm new password")]
        [Compare("NewPassword", ErrorMessage = "The new password and confirmation password do not match.")]
        public string ConfirmPassword { get; set; }

        public string StatusMessage { get; set; }

        public string PublisherName { get; set; }
    }
}
