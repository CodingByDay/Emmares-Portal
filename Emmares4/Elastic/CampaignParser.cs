using System;
using System.Threading;

using Emmares4.Helpers;

namespace Emmares4.Elastic
{
    public class CampaignParser
    {
        private static DateTime cycleTime = new DateTime(2000, 1, 1);
        public static void Run ()
        {
            Thread.Sleep(60000);
            Log.Write("CampaignParser start");

            while (true)
            {
                try
                {
                    Thread.Sleep(30000);

                    var thisCycleTime = cycleTime;
                    cycleTime = DateTime.UtcNow.AddHours(-1);

                    var updated = DBReader<CampaignContent>.Read("select ID, (select Name from ContentTypes ct where ct.ID = ContentTypeID), (select Name from FieldsOfInterest fi where fi.ID = FieldOfInterestID), Name, (select Name from Providers p where p.ID = PublisherID), (select Code + ' ' + Name from Regions r where r.ID = RegionID), OptInLink, OptOutLink, Newsletter from Campaigns where DateModified > @lastScanTime", (r) =>
                   {
                       var id = r.GetGuid(0).ToString();
                       var ct = r.IsDBNull(1) ? "" : r.GetString(1);
                       var fi = r.IsDBNull(2) ? "" : r.GetString(2);
                       var name = r.IsDBNull(3) ? "" : r.GetString(3);
                       var p = r.IsDBNull(4) ? "" : r.GetString(4);
                       var rgn = r.IsDBNull(5) ? "" : r.GetString(5);
                       var inLink = r.IsDBNull(6) ? "" : r.GetString(6);
                       var outLink = r.IsDBNull(7) ? "" : r.GetString(7);
                       var newsletter = r.IsDBNull(8) ? "" : r.GetString(8);

                       return new CampaignContent(id, ct + " " + fi + " " + name + " " + p + " " + rgn + " " + inLink + " " + outLink, newsletter);
                   }, (p) =>
                   {
                       p.Add(DBParameter.DateTime("lastScanTime", thisCycleTime));
                   });

                    foreach (var cc in updated)
                    {
                        CampaignContentES.UpdateDocument(cc.ID, cc.Content);
                    }

                    if (updated.Count > 0)
                    {
                        Log.Write ("CampaignParser updated " + updated.Count.ToString () + " ElasticSearch documents");
                    }
                } catch (Exception ex)
                {
                    var dbg = ex.ToString();
                    Log.Write("CampaignParser cycle failed: " + ex.ToString());
                }
            }
        }
    }
}
