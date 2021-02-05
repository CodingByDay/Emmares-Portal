using Emmares4.Data;
using Microsoft.AspNetCore.Identity;
using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;

namespace Emmares4.Models.HomeViewModels
{
    public class DashboardViewModel
    {

        ApplicationDbContext _context;
        DbConnection _dbConnection;

        public DashboardViewModel(ApplicationDbContext context, DbConnection dbConnection, 
            UserManager<ApplicationUser> userManager, ApplicationUser user, ChartInterval interval)
        {
            _context = context;
            _dbConnection = dbConnection;

            var sb = user.Balance.ToString("F");

            var separator = '.';

            var i = sb.IndexOf('.');
            if (i < 0)
            {
                separator = ',';
                i = sb.IndexOf(',');
            }

            string sf = string.Empty, sp = string.Empty;
            if (i > 0)
            {
                sf = sb.Substring(0, sb.IndexOf(separator));
                sp = sb.Substring(sb.IndexOf(separator) + 1);
                BalanceFullPart = int.Parse(sf);
                BalancePartialPart = int.Parse(sp);
            }

            var interests = _context.Entry(user)
                 .Collection(s => s.Interests)
                 .Query()
                 .Select(p => p.FieldOfInterest.Name)
                 .ToList();

            FavouriteFields = interests;

            ChartData = new ChartViewModel(context, _dbConnection, user, interval, "GetChartData", "GetChartStats");
        }

        public int BalanceFullPart { get; set; }
        public int BalancePartialPart { get; set; }
        public List<string> FavouriteFields { get; set; }
      
        public ChartViewModel ChartData { get; set; }
    }
}
