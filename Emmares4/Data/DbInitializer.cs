using Emmares4.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Emmares4.Data
{
    public class DbInitializer
    {
        public static void Initialize(ApplicationDbContext context)
        {
            context.Database.EnsureCreated();

            if (context.Users.Any())
            {
                return;   // DB has been seeded
            }

            var users = new List<ApplicationUser>()
            {
                new ApplicationUser(){  Email = "User1@emmares.com", UserName="User1", Balance = 92.47 },
                new ApplicationUser(){  Email = "User2@emmares.com", UserName="User2", Balance = 92.47 },
                new ApplicationUser(){  Email = "User3@emmares.com", UserName="User3", Balance = 92.47 },
                new ApplicationUser(){  Email = "User4@emmares.com", UserName="User4", Balance = 92.47 },
                new ApplicationUser(){  Email = "User5@emmares.com", UserName="User5", Balance = 92.47 },
            };
            users.ForEach(u => context.Users.Add(u));
            context.SaveChanges();


            var campaigns = new List<Campaign>()
            {
                new Campaign(){  },
            };
        }
    }
}
