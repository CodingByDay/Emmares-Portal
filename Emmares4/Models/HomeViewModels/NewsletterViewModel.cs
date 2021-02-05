using Emmares4.Data;
using Microsoft.AspNetCore.Identity;
using System.Collections.Generic;
using System.Data.Common;

using Emmares4.Helpers;

namespace Emmares4.Models.HomeViewModels
{
    public class NewsletterViewModel    // A specific newsletter.
    {
        public NewsletterViewModel(string CampaignID)
        {
            Newsletter = DBReader<string>.Read("select Newsletter from [dbo].[Campaigns] where ID = @id", (r) =>
            //Newsletter = DBReader<string>.Read("select Newsletter from [dbo].[Campaigns] where ID = '300B50F0-15EE-4246-7E72-08D5C78A4A5A'", (r) =>
            {
                return r.IsDBNull(0) ? "" : r.GetString(0); // Returns empty string, if value is null.
            }, (p) =>
            {
                p.Add(DBParameter.String("id", CampaignID));
            })[0];
        }
        public string Newsletter { get; set; }
    }
}
