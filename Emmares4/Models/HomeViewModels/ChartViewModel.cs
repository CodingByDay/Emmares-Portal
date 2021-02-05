using Dapper;
using Emmares4.Data;
using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Linq;
using System.Threading.Tasks;

namespace Emmares4.Models.HomeViewModels
{
    public class ChartViewModel
    {
        public ChartInterval Interval { get; set; }
        public List<string> XLables { get; set; }
        public List<double> Values { get; set; }
        public Dictionary<string, object> ItemsToDisplay { get; set; }

        private Dictionary<ChartInterval, int> IntervalDays = new Dictionary<ChartInterval, int>()
        {
            { ChartInterval.Week, 7 },
            { ChartInterval.Month, 30 },
            { ChartInterval.Year, 365 }
        };

        DbConnection _dbConnection;
        public ChartViewModel(ApplicationDbContext context, DbConnection dbConnection, ApplicationUser user, ChartInterval interval, string chartSP, string chartStatsSP, List<string> campaign = null)
        {
            Interval = interval;

            _dbConnection = dbConnection;

            object param;
            if (campaign != null && campaign.Count != 0)   // Not all queries expect campID, so we have to omit for some.
                param = new { chartType = (int)interval, user = user.Id, campID = campaign[0] };    // Currently picks 1st campaign. Expand to pick selected one once implemented.
            else
                param = new { chartType = (int)interval, user = user.Id };

            var data = _dbConnection.Query(chartSP, param,
                null, true, null, System.Data.CommandType.StoredProcedure)
                .ToDictionary(x=>x.Label, x=>x.Ratings);

            XLables = data.Keys.Cast<string>().ToList();
            Values = data.Values.Cast<double>().ToList();

            var stats = _dbConnection.Query(chartStatsSP, param,
               null, true, null, System.Data.CommandType.StoredProcedure);

            
            if (stats.Any())
                ItemsToDisplay = new Dictionary<string, object>(stats.First() as IDictionary<string, object>);  // Keys are values defined in .sql file
        }
    }

    public enum ChartInterval
    {
        Daily,
        Week,
        Month,
        Year
    }
}
