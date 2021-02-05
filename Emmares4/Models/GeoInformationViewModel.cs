using Emmares4.Helpers;
using Microsoft.AspNetCore.Mvc.Rendering;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Emmares4.Models
{
    public class GeoInformationViewModel
    {
        public Dictionary<string, int> CitiesCount { get; set; }
        public string SelectedCampaign { get; set; }
        public List<SelectListItem> ListCampaigns { get; set; }
        public string SelectedStartDate { get; set; }
        public List<SelectListItem> ListStartDates { get; set; }
        public string SelectedEndDate { get; set; }
        public List<SelectListItem> ListEndDates { get; set; }
        public List<string[]> GeoInformation { get; set; }

        public GeoInformationViewModel(string user) // Parameter is user's (Marketeer's) ID
        {
            GeoInformation = DBReader<string[]>.Read(
                "select c.Name, ch.City, ch.Country, ch.Time from [dbo].[CampaignsHits] as ch " +
                "inner join[dbo].[Campaigns] as c on ch.CampaignID = c.ID " +
                "inner join[dbo].[Providers] as p on c.PublisherID = p.ID " +
                "where p.OwnerId = cast(@user as uniqueidentifier) " +
                "and ch.Country is not NULL;", (r) =>                           // Mainly to filter out entries added before implementation. Probably not needed otherwise, as Country shouldn't be NULL.
            {
                return new string[] { r.GetString(0), r.GetString(1), r.GetString(2), r.GetDateTime(3).ToShortDateString() };
            }, (p) =>
            {
                p.Add(DBParameter.String("user", user));
            });

            CountOccurences(GeoInformation);

            PopulateSelectLists(GeoInformation);
        }

        private void CountOccurences(List<string[]> data)
        {
            List<string> list = new List<string>();
            foreach(var row in data)
            {
                list.Add(row[2]);
            }

            var q = from x in list
                    group x by x into g
                    let count = g.Count()
                    orderby count descending
                    select new { Value = g.Key, Count = count };
            CitiesCount = new Dictionary<string, int>();
            foreach (var x in q)
            {
                CitiesCount.Add(x.Value, x.Count);
            }
        }

        private void PopulateSelectLists(List<string[]> GeoInformation)
        {
            ListCampaigns = new List<SelectListItem>();
            ListStartDates = new List<SelectListItem>();
            ListEndDates = new List<SelectListItem>();

            ListCampaigns.Add(new SelectListItem
            {
                Text = "All",
                Value = "All"
            });
            foreach (var row in GeoInformation)    // CampaignName, City, Country, Time
            {
                if (!ListCampaigns.Exists(x => x.Value == row[0]))  // If the campaign name isn't in the dropdown menu yet.
                {
                    ListCampaigns.Add(new SelectListItem
                    {
                        Text = row[0],
                        Value = row[0]
                    });
                }
                if (!ListStartDates.Exists(x => x.Value == row[3]))   // Add all valid dates
                {
                    ListStartDates.Add(new SelectListItem
                    {
                        Text = row[3],
                        Value = row[3]
                    });
                    ListEndDates.Add(new SelectListItem
                    {
                        Text = row[3],
                        Value = row[3]
                    });
                }
            }

            try
            {
                SelectedEndDate = ListStartDates.Last().Value;
            }
            catch
            {
                SelectedEndDate = DateTime.Now.ToShortDateString();
            }
        }
    }
}
