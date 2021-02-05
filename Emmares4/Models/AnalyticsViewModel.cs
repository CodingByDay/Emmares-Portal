using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Linq;
using System.Security.Claims;
using Emmares4.Data;
using Emmares4.Models;
using Emmares4.Models.HomeViewModels;
using Microsoft.AspNetCore.Identity;
using Emmares4.Helpers;

namespace Emmares4.Models
{
    public class AnalyticsViewModel
    {
        private ApplicationDbContext _context;
        private UserManager<ApplicationUser> _userManager;
        private ClaimsPrincipal user;

        public AnalyticsViewModel(ApplicationDbContext db, DbConnection dbConnection, 
            UserManager<ApplicationUser> userManager, ApplicationUser user, ChartInterval interval)
        {
            _context = db;
            _userManager = userManager;

            var campaigns = DBReader<string>.Read("select cast(c.ID as varchar(36)) from [dbo].[Campaigns] as c inner join [dbo].[Providers] as p on c.PublisherID = p.ID where p.OwnerID = @id", (r) =>
            {
                return r.IsDBNull(0) ? null : r.GetString(0);
            }, (p) =>
            {
                p.Add(DBParameter.String("id", user.Id));
            });

            ChartData = new ChartViewModel(db, dbConnection, user, interval, "Marketeer_GetChartData", "Marketeer_GetChartStats");

            ChartDataDisplays = new ChartViewModel(db, dbConnection, user, interval, "GetChartCampaignsHitsData", "GetChartCampaignsHitsStats", campaigns);

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

            var pub = _context.Providers.Where(x => x.Owner == user).FirstOrDefault();

            if (pub == null)
                return;

            IsProviderSet = true;

            _context
                .Entry(pub)
                .Collection(s => s.Campaigns)
                .Load();

            if (pub.Campaigns.Any())
            {
                RatedCampaigns = pub.Campaigns.Count();

                var budget = pub.Campaigns.Sum(x => x.Budget);

                if (_context.Statistics.Any())
                {
                    var cs = (from c in pub.Campaigns select c).ToList();
                    var stats = _context.Statistics.ToList ().Where (s => s.Campaign != null).ToList();
                    // TODO                         *********
                    // TODO huh, bug v EF ali indijci kaj narobe nastavili? 
                    // TODO mora biti ToList() po Statistics, sicer se spodaj narobe filtrira???!!!

                    /* TODO se sploh ne rabi, rewarding pool je samo sestevek odprtih budgetov?
                    var rewardPoolQ = (from c in pub.Campaigns
                                           //join s in _context.Statistics on c.ID equals s.Campaign.ID
                                       join s in stats on c.ID equals s.Campaign.ID
                                       select s.Reward).ToList ();

                    var rewardPool = rewardPoolQ.Sum();
                    */

                    //AverageScore = (from c in pub.Campaigns
                    //                join s in _db.Statistics on c.ID equals s.Campaign.ID
                    //                select s.Rating).Average();

                    //Evaluations = (from c in pub.Campaigns
                    //               join s in _db.Statistics on c.ID equals s.Campaign.ID
                    //               select s.ID).Count();

                    RewardPoolStatus = budget; // - rewardPool;

                    var myHighestRatingsAverage = (from c in pub.Campaigns
                                                       //join s in _context.Statistics on c.ID equals s.Campaign.ID
                                                   join s in stats on c.ID equals s.Campaign.ID
                                                   join cnt in _context.ContentTypes on c.ContentTypeID equals cnt.ID
                                                   group s by new { Publisher = pub.Name, s.Campaign.ID, cnt.Name } into g
                                                   orderby g.Average(x => x.Rating) descending
                                                   select new { pub = g.Key.Publisher, tp = g.Key.Name, score = g.Average(x => x.Rating) })
                                                   .FirstOrDefault();

                    List<Tuple<string, string, double>> standings = new List<Tuple<string, string, double>>();

                    if (myHighestRatingsAverage != null)
                    {
                        standings.Add(new Tuple<string, string, double>(myHighestRatingsAverage.pub, myHighestRatingsAverage.tp, myHighestRatingsAverage.score));

                        var all = (from c in _context.Campaigns
                                       //join s in _context.Statistics on c.ID equals s.Campaign.ID
                                   join s in stats on c.ID equals s.Campaign.ID
                                   join cnt in _context.ContentTypes on c.ContentTypeID equals cnt.ID
                                   where c.ContentType.Name == myHighestRatingsAverage.tp && pub.Name != myHighestRatingsAverage.pub
                                   group s by new { Publisher = pub.Name, s.Campaign.ID, cnt.Name } into g
                                   orderby g.Average(x => x.Rating) descending
                                   select new { pub = g.Key.Publisher, tp = g.Key.Name, score = g.Average(x => x.Rating) })
                                   .ToList();

                        if (all.Any())
                            all.ForEach(x => standings.Add(new Tuple<string, string, double>(x.pub, x.tp, x.score)));
                    }

                    Standings = standings;
                }
            }
        }

        public bool IsProviderSet { get; set; }
        public int BalanceFullPart { get; set; }
        public int BalancePartialPart { get; set; }
        public int RatedCampaigns { get; set; }
        public double RewardPoolStatus { get; set; }
        public ChartViewModel ChartData { get; set; }
        public ChartViewModel ChartDataDisplays { get; set; }
        public List<Tuple<string, string, double>> Standings { get; set; } = new List<Tuple<string, string, double>>();
    }
}