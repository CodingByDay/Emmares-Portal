using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Emmares4.Models;

namespace Emmares4.Data
{
    public class ApplicationDbContext : IdentityDbContext<ApplicationUser>
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options)
        {
        }

        public ApplicationDbContext()
        {
        }

        protected override void OnModelCreating(ModelBuilder builder)
        {
            builder.Entity<CampaignsHasKeywords>()
                .HasKey(bc => new { bc.CampaignID, bc.KeyWordID });

            builder.Entity<CampaignsHasKeywords>()
                .HasOne(p => p.campaignset)
                .WithMany(x => x.keyWordsset)
                .HasForeignKey(y => y.CampaignID);

            builder.Entity<CampaignsHasKeywords>()
                .HasOne(p => p.keyWordsset)
                .WithMany(x => x.campaignsset)
                .HasForeignKey(y => y.KeyWordID);

            base.OnModelCreating(builder);
        }

        public virtual DbSet<Campaign> Campaigns { get; set; }
        public virtual DbSet<Statistic> Statistics { get; set; }
        public virtual DbSet<FieldOfInterest> FieldsOfInterest { get; set; }
        public virtual DbSet<Region> Regions { get; set; }
        public virtual DbSet<ContentType> ContentTypes { get; set; }
        public virtual DbSet<UserInterest> UserInterests { get; set; }
        public virtual DbSet<Subscription> Subscriptions { get; set; }
        public virtual DbSet<Provider> Providers { get; set; }
        public virtual DbSet<SubscriptionsLog> SubscriptionsLog { get; set; }
        public virtual DbSet<KeyWords> KeyWords { get; set; }
        public virtual DbSet<CampaignsHasKeywords> CampaignsHasKeywords { get; set;  }

    }
}
