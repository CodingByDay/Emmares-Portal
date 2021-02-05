using System;

namespace Emmares4.Models.HomeViewModels
{
    public class ProviderViewModel
    {
        public int ProviderID { get; set; }
        public string Provider { get; set; }
        public int Subscriptions { get; set; }
        public int FieldOfInterestID { get; set; }
        public string FieldOfInterest { get; set; }
        public double AverageRating { get; set; }
        public DateTime AddedOn { get; set; }
        public int RecipientCount { get; set; }
        public int Records { get; set; }
        public bool IsSubscribed { get; set; }
        public string Campaign { get; set; }
        public bool Starred { get; set; }
        public string keyword { get; set; }
        public string AverageRatingStr { get { return this.AverageRating.ToString("###,##0.00"); } }
        public string CampaignID { get; set; }
        public int HasNewsletter { get; set; }
    }
}
