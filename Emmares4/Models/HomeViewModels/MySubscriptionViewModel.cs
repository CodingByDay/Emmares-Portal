using System;
using Emmares4.Data;
using System.Linq;
using Microsoft.AspNetCore.Identity;
using System.Collections.Generic;
using System.Data.Common;
using Dapper;

using Emmares4.Helpers;

namespace Emmares4.Models.HomeViewModels
{
    public class MySubscriptionViewModel
    {
        private ApplicationDbContext _context;
        private UserManager<ApplicationUser> _userManager;        
        private readonly DbConnection _dbConnection;

        public MySubscriptionViewModel(ApplicationDbContext context, UserManager<ApplicationUser> userManager,
            ApplicationUser user, DbConnection dbConnection)
        {
            _dbConnection = dbConnection;
            _context = context;
            _userManager = userManager;
            _context = context;

            var favorite = SetFavoriteDictionary.Get(user.Id);

            var favoriteID = DBReader<int>.Read("select ID from dbo.FieldsOfInterest where Name = @name", (r) =>
            {
                return r.GetInt32(0);
            }, (p) =>
            {
                p.Add(DBParameter.String("name", favorite));
            }).FirstOrDefault();

            var interestProviders = DBReader<Tuple<int,string>>.Read("select ID, Name from dbo.Providers where ID in (select distinct(PublisherID) from dbo.Campaigns where FieldOfInterestID = @interest)", (r) =>
            {
                return new Tuple<int, string>(r.GetInt32(0), r.GetString(1));
            }, (p) => {
                p.Add(DBParameter.Int("interest", favoriteID));
            });

            InterestProviders = interestProviders
                .Select(ic =>
                {
                    var subscribed = DBReader<int>.Read("select count(ID) from Subscriptions where SubscriberId = @userID and ProviderID = @provider and FieldOfInterestID = @interest and OptInStatus = @optStatus", (r) =>
                    {
                        return r.GetInt32(0);
                    }, (p) =>
                    {
                        p.Add(DBParameter.String("userID", user.Id));
                        p.Add(DBParameter.Int("provider", ic.Item1));
                        p.Add(DBParameter.Int("interest", favoriteID));
                        p.Add(DBParameter.Int("optStatus",1));
                    }).First() > 0;
                    var rating = DBReader<double>.Read("select avg(cast(Rating as float)) from [dbo].[Statistics] where (CampaignID in (select ID from Campaigns where PublisherID = @provider and FieldOfInterestID = @interest))", (r) =>
                    {
                        return r.IsDBNull (0) ? 0.0 : r.GetDouble(0);
                    }, (p) =>
                    {
                        p.Add(DBParameter.Int("provider", ic.Item1));
                        p.Add(DBParameter.Int("interest", favoriteID));
                    }).First();
                    return new ProviderViewModel {
                        Provider = ic.Item2,
                        IsSubscribed = subscribed,
                        AverageRating = rating,
                        ProviderID = ic.Item1,
                        FieldOfInterestID = favoriteID
                    };
                })
                .OrderByDescending(x => (x.IsSubscribed ? 100000 : 0) + x.AverageRating)
                .ToList();

            var myProviders = DBReader<int>.Read("select distinct(ProviderID) from Subscriptions where SubscriberId = @userID and OptInStatus = @optStatus", (r) =>
            {
                return r.GetInt32(0);
            }, (p) =>
            {
                p.Add(DBParameter.String("userID", user.Id));
                p.Add(DBParameter.Int("optStatus", 1));
            });
            MySubscriptions = myProviders.Select(mp =>
            {
                var name = DBReader<string>.Read("select Name from Providers where ID = @provider", (r) =>
                {
                    return r.IsDBNull(0) ? "Unknown" : r.GetString(0);
                }, (p) =>
                {
                    p.Add(DBParameter.Int("provider", mp));
                }).First();
                var rating = DBReader<double>.Read("select avg(cast(Rating as float)) from [dbo].[Statistics] where (CampaignID in (select ID from Campaigns where PublisherID = @provider))", (r) =>
                {
                    return r.IsDBNull(0) ? 0.0 : r.GetDouble(0);
                }, (p) =>
                {
                    p.Add(DBParameter.Int("provider", mp));
                    p.Add(DBParameter.Int("interest", favoriteID));
                }).First();
                return new ProviderViewModel
                {
                    Provider = name,
                    AverageRating = rating,
                    ProviderID = mp
                };
            })
            .OrderByDescending (ms => ms.AverageRating)
            .ToList ();

            var lastFavorite = SetFavoriteDictionary.Get(user.Id);
            Favorite = lastFavorite != null ? lastFavorite : favorite;
        }

        public string Favorite { get; set; }
        public List<ProviderViewModel> InterestProviders { get; set; }
        public List<ProviderViewModel> MySubscriptions { get; set; }
    }

    public class ProviderViewModelComparer : IEqualityComparer<ProviderViewModel>
    {
        public bool Equals(ProviderViewModel x, ProviderViewModel y)
        {
            return x.ProviderID == y.ProviderID;
        }

        public int GetHashCode(ProviderViewModel obj)
        {
            return obj.GetHashCode();
        }
    }
}