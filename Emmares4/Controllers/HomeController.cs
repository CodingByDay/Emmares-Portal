using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Emmares4.Models;
using Emmares4.Data;
using Emmares4.Models.HomeViewModels;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;

using System.IO;
using Microsoft.AspNetCore.Http;
using System.Net.Http.Headers;
using System.Data.Common;
using Dapper;

using Emmares4.Helpers;
using Emmares4.Elastic;

using System.Data.SqlClient;
using System.Data;

using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Processing;
using SixLabors.ImageSharp.Processing.Transforms;

namespace Emmares4.Controllers
{

    public class HomeController : Controller
    {
        private readonly SignInManager<ApplicationUser> _signInManager;
        private readonly ILogger _logger;
        ApplicationDbContext _db;
        UserManager<ApplicationUser> _userManager;
        DbConnection _dbConnection;
        ApplicationUser _user;
        private IHttpContextAccessor _accessor;
        private const string serverIP = "195.95.161.252";

        public HomeController(ApplicationDbContext dbContext, UserManager<ApplicationUser> userManager,
                                ILogger<HomeController> logger, SignInManager<ApplicationUser> signInManager,
                                DbConnection dbConnection, IHttpContextAccessor accessor)
        {
            _dbConnection = dbConnection;
            _db = dbContext;
            _userManager = userManager;
            _logger = logger;
            _signInManager = signInManager;
            _accessor = accessor;
        }
        [HttpPost]
        public async Task<IActionResult> UploadImage(IFormFile file)
        {
            var fileName = ContentDispositionHeaderValue
            .Parse(file.ContentDisposition)
            .FileName
            .Trim('"');
            _user = _userManager.GetUserAsync(User).Result;
            if (fileName.Contains(".jpg") || fileName.Contains(".png") || fileName.Contains(".jpeg") || fileName.Contains(".gif"))
            {
                fileName = _user.Id + ".jpg";

                if (file.Length > 0)
                {
                    using (var stream = new MemoryStream())
                    {
                        await file.CopyToAsync(stream);
                        stream.Seek(0, SeekOrigin.Begin);       // Reset stream.
                        using (var image = Image.Load(stream))
                        {
                            image.Mutate(x => x.Resize(image.Width * 200 / image.Height, 200)); // Resizes image to 200 width and proportional height.
                            image.Save(Path.Combine($"wwwroot/images", fileName));
                        }
                    }
                }
            }
            else
            {
                return RedirectToAction("AccountSettings", new { wasRedirected = true });
            }

            return RedirectToAction(nameof(AccountSettings));
        }

        public IActionResult Dashboard(int chartType = 1)
        {
            if (_user == null)
                _user = _userManager.GetUserAsync(User).Result;
            if (_user == null)
                return RedirectToAction("Login", "Account");
            //if (User.IsInRole("Marketeer"))
            //{
            //    return RedirectToAction("Index", "Campaigns");
            //}
            var viewModel = new DashboardViewModel(_db, _dbConnection, _userManager, _user, (ChartInterval)chartType);
            return View(viewModel);
        }

        // Has to have parameters, even if ignores them as there's only one chart on Recipient's page right now.
        public IActionResult GetChart(int chartType, string view, string sql_file_data, string sql_file_stats)
        {
            if (_user == null)
                _user = _userManager.GetUserAsync(User).Result;
            var viewModel = new ChartViewModel(_db, _dbConnection, _user, (ChartInterval)chartType, "GetChartData", "GetChartStats");
            return PartialView("_chartView", viewModel);
        }

        public async Task<IActionResult> GetProviders()
        {
            if (_user == null)
                _user = _userManager.GetUserAsync(User).Result;

            var popularity = Request.Form["Popularity"].ToString();
            var contentType = Request.Form["ContentType"].ToString();
            var interval = Request.Form["Interval"].ToString();
            var foi = Request.Form["FieldOfInterest"].ToString();
            var region = Request.Form["region"].ToString();
            var keyword = Request.Form["keyword"].ToString();

            var start = Request.Form["start"].ToString();
            var draw = Request.Form["draw"].ToString();

            /*
            var query = @"Select p.ID as ProviderID, p.Name as Provider, f.Id as FieldOfInterestID, f.Name as FieldOfInterest, cmp.Name as Campaign, count(sub.ID) subscriptions, 
                            cast(AVG(cast(Rating as decimal)) as numeric(10,2)) as AverageRating, count(s.ID) as Stats, sum(cmp.Recipients) as RecipientCount,
                            convert(nvarchar(36), cmp.ID) as CampaignID, cmp.HasNewsletter as HasNewsletter, Records = COUNT(*) OVER()
                        from Providers p
                        left outer join Campaigns cmp on cmp.PublisherID = p.ID
                         left join CampaignsHasKeywords chk on cmp.ID = chk.CampaignID
                         left  join KeyWords k on  chk.KeyWordID = k.ID
                        inner join ContentTypes cnt on cmp.ContentTypeID = cnt.ID
                        inner join FieldsOfInterest f on cmp.FieldOfInterestID = f.ID
                        inner join Regions r on cmp.RegionID = r.ID
                        left outer join [Statistics] s on s.CampaignID = cmp.ID
                        left outer join Subscriptions sub on p.ID = sub.ProviderID and cmp.ContentTypeID = sub.ContentTypeID
                        {0}
                        group by p.ID, p.Name, f.ID, f.Name, cmp.Name, p.DateAdded, cmp.ID, cmp.HasNewsletter
                        {1} 
                        OFFSET {2} ROWS FETCH FIRST 10 ROWS ONLY";

            var orderByClause = string.IsNullOrEmpty(popularity) ? " AverageRating Desc " : $" {popularity} Desc ";
            orderByClause = $"ORDER BY {orderByClause}, p.DateAdded Desc";

            var whereClause = string.Empty;
            if (!string.IsNullOrEmpty(contentType))
                whereClause = string.IsNullOrEmpty(contentType) ? "" : $" cnt.ID = {contentType} ";

            if (!string.IsNullOrEmpty(foi))
                whereClause = (string.IsNullOrEmpty(whereClause) ? "" : $" {whereClause}  AND ") + $" f.ID = {foi} ";

            if (!string.IsNullOrEmpty(region))
                whereClause = (string.IsNullOrEmpty(whereClause) ? "" : $" {whereClause}  AND ") + $"  r.ID = {region} ";

            if (!string.IsNullOrEmpty(keyword))
                whereClause = (string.IsNullOrEmpty(whereClause) ? "" : $" {whereClause}  AND ") + $"  k.KeyWord like '%{keyword}%' ";

            if (!string.IsNullOrEmpty(interval))
                whereClause = (string.IsNullOrEmpty(whereClause) ? "" : $" {whereClause}  AND ")
                    + $" (p.DateAdded BETWEEN DATEADD(day, -{interval}, GETDATE()) and GETDATE() ) ";

            whereClause = string.IsNullOrEmpty(whereClause) ? "" : " WHERE " + whereClause;

            query = string.Format(query, whereClause, orderByClause, start);

            var providers = await _dbConnection.QueryAsync<ProviderViewModel>(query);
            */

            try
            {
                var ids = CampaignContentES.ListDocuments(keyword);

                var query = @"Select p.ID as ProviderID, p.Name as Provider, f.Id as FieldOfInterestID, f.Name as FieldOfInterest, cmp.Name as Campaign, count(sub.ID) subscriptions, 
                            cast(AVG(cast(Rating as decimal)) as numeric(10,2)) as AverageRating, count(s.ID) as Stats, sum(cmp.Recipients) as RecipientCount,
                            convert(nvarchar(36), cmp.ID) as CampaignID, cmp.HasNewsletter as HasNewsletter, Records = COUNT(*) OVER()
                        from Providers p
                        left outer join Campaigns cmp on cmp.PublisherID = p.ID
                         left join CampaignsHasKeywords chk on cmp.ID = chk.CampaignID
                         left  join KeyWords k on  chk.KeyWordID = k.ID
                        inner join ContentTypes cnt on cmp.ContentTypeID = cnt.ID
                        inner join FieldsOfInterest f on cmp.FieldOfInterestID = f.ID
                        inner join Regions r on cmp.RegionID = r.ID
                        left outer join [Statistics] s on s.CampaignID = cmp.ID
                        left outer join Subscriptions sub on p.ID = sub.ProviderID and cmp.ContentTypeID = sub.ContentTypeID
                        group by p.ID, p.Name, f.ID, f.Name, cmp.Name, p.DateAdded, cmp.ID, cmp.HasNewsletter";

                var providersAsync = await _dbConnection.QueryAsync<ProviderViewModel>(query);
                var providers = providersAsync.Where(p => ids.Contains(p.CampaignID.ToLower())).Skip(Convert.ToInt32(start)).Take(10).ToList();

                var first = providers.FirstOrDefault();
                int recordCount = 0;
                if (first != null)
                    recordCount = first.Records;

                // TODO prva dva oznacimo z zvezdico, dokler logika ni urejena
                /*
                providers.Take(2).ToList().ForEach(pr =>
                {
                    pr.Starred = true;
                });
                */

                int count = 0;
                providers.ForEach(pr =>
                {
                    string address = _accessor.HttpContext.Connection.RemoteIpAddress.ToString();
                    if (address == "::1")  // Localhost for testing.
                        address = serverIP;

                    //string[] location = CityStateCountByIp(address);

                    // Add row to table tracking hits.

                    DBWriter.Write("insert into CampaignsHits (CampaignID, Time, Position) values (@CampID, @Time, @Position)", (p) =>
                        {
                            p.Add(DBParameter.String("CampID", pr.CampaignID));
                            DateTime now = DateTime.Now;
                            now = now.AddSeconds(-now.Second);
                            p.Add(DBParameter.DateTime("Time", now));
                            p.Add(DBParameter.Int("Position", ++count));
                        //   p.Add(DBParameter.String("IP", address));
                    });

                    // check where user is subscribed
                    var subscribed = DBReader<int>.Read("select count(ID) from Subscriptions where SubscriberId = @userID and ProviderID = @provider and FieldOfInterestID = @interest and OptInStatus = @optStatus", (r) =>
                        {
                            return r.GetInt32(0);
                        }, (p) =>
                        {
                            p.Add(DBParameter.String("userID", _user.Id));
                            p.Add(DBParameter.Int("provider", pr.ProviderID));
                            p.Add(DBParameter.Int("interest", pr.FieldOfInterestID));
                            p.Add(DBParameter.Int("optStatus", 1));
                        }).First() > 0;
                    pr.IsSubscribed = subscribed;
                });

                return Json(new { draw, recordsTotal = recordCount, recordsFiltered = recordCount, data = providers });
            }
            catch (Exception ex)
            {
                var dbg = ex.ToString();
                return Json(new { draw, recordsTotal = 0, recordsFiltered = 0, data = new List<ProviderViewModel> () });
            }
        }

        Dictionary<string, string> _popularityMap = new Dictionary<string, string>()
        {
                { "Best rated" , "AverageRating"},
                { "Most subscribed", "Subscriptions" },
                { "Most ratings", "Stats" },
                { "Most sent", "RecipientCount" }
        };

        Dictionary<string, string> _intervalMap = new Dictionary<string, string>()
        {
                { "All Time", "0" },
                { "This Week" , "6"},
                { "This Month", "29" },
                { "This Year", "364" }
        };

        public static string[] CityStateCountByIp(string IP)
        {
            // TODO Needs reimplementation as it is unreliable! Perhaps better to add it to Newsletter service or create a seperate service.
            string key; // Initialise it as server key
            if (serverIP == "89.212.55.202")    // Server's IP.
                key = "PASTE KEY HERE";         // Can also initialise the key and change the check though benefit of that is negligible.
            else if (serverIP == "195.95.161.252")
                key = "0917563a0e436f7c1962aaae2ae7314fb84ae2038d4ce28e1a313bc5d02a55c1";   // key for public IP 195.95.161.252
            else
                throw new ApplicationException(String.Format("IP and key combination are unrecognised.\nIP:\t{0}\nKey:\t{1}", serverIP, key));

            var url = String.Format("http://api.ipinfodb.com/v3/ip-city/?key={0}&ip={1}", key, IP);
            var request = System.Net.WebRequest.Create(url);

            using (System.Net.WebResponse wrs = request.GetResponse())
            using (Stream stream = wrs.GetResponseStream())
            using (StreamReader reader = new StreamReader(stream))
            {
                string[] data = reader.ReadToEnd().Split(';');  // Sample query: OK;;195.95.161.252;SI;Slovenia;Velenje;Velenje;3320;46.3592;15.1103;+02:00
                return new string[] { data[4], data[5] };       // Returns country + city
            }
        }

        public IActionResult MyContent()
        {
            if (_user == null)
                _user = _userManager.GetUserAsync(User).Result;

            // preset selected favorite
            var fav = SetFavoriteDictionary.Get(_user.Id);
            if (fav == null)
            {
                var interests = _db.Entry(_user)
                   .Collection(s => s.Interests)
                   .Query()
                   .Select(p => p.FieldOfInterest.Name)
                   .ToList();
                if (interests.Count > 0)
                {
                    SetFavoriteDictionary.Set(_user.Id, interests[0]);
                }
            }

            var viewModel = new MyContentViewModel(_db, _userManager, _user, _dbConnection);
            PopulateGridFields();
            return View(viewModel);
        }

        public IActionResult DeleteField(string FieldOfInterestID)
        {
            if (_user == null)
                _user = _userManager.GetUserAsync(User).Result;

            PopulateGridFields();

            var interest = _db.FieldsOfInterest.Where(c => c.Name == FieldOfInterestID).FirstOrDefault();

            _db.Entry(_user)
                .Collection(x => x.Interests)
                .Load();

            UserInterest userInterest = null;

            if (_user != null && interest != null)
                userInterest = _user.Interests.Where(ui => ui.User == _user && ui.FieldOfInterest == interest).FirstOrDefault();

            if (userInterest != null)
            {
                _user.Interests.Remove(userInterest);
                _db.SaveChanges();
            }

            return RedirectToAction(nameof(MyContent));

        }
        public IActionResult AddField(int FieldOfInterestID)
        {
            if (_user == null)
                _user = _userManager.GetUserAsync(User).Result;

            PopulateGridFields();

            var interest = _db.FieldsOfInterest.Where(c => c.ID == FieldOfInterestID).FirstOrDefault();

            if (interest != null)
            {
                _db.Entry(_user)
                .Collection(x => x.Interests)
                .Load();

                var interestExists = _user.Interests.Where(c => c.User == _user && c.FieldOfInterest == interest).FirstOrDefault();
                if (interestExists == null)
                {
                    _user.Interests.Add(new UserInterest() { User = _user, FieldOfInterest = interest });
                    _db.SaveChanges();
                }
            }

            var contentTypeQuery = from c in _db.ContentTypes
                                   orderby c.Name
                                   select c;
            ViewBag.ContentTypeID = new SelectList(contentTypeQuery.AsNoTracking(), "ID", "Name", null);

            var vm = new FavoritesViewModel(_db, _userManager, _user);

            return PartialView("_favorites", vm);
        }

        private void PopulateGridFields(object selectedContentType = null, object selectedFieldOfInterest = null, object selectedRegion = null)
        {



            ViewBag.Popularity = new SelectList(_popularityMap, "Value", "Key");
            ViewBag.Interval = new SelectList(_intervalMap, "Value", "Key");

            var contentTypeQuery = from c in _db.ContentTypes
                                   orderby c.Name
                                   select c;
            ViewBag.ContentTypeID = new SelectList(contentTypeQuery.AsNoTracking(), "ID", "Name", selectedContentType);

            var foiQuery = from c in _db.FieldsOfInterest
                           orderby c.Name
                           select c;
            ViewBag.FieldOfInterestID = new SelectList(foiQuery.AsNoTracking(), "ID", "Name", selectedFieldOfInterest);

            var regionQuery = from c in _db.Regions
                              orderby c.Name
                              select c;
            ViewBag.RegionID = new SelectList(regionQuery.AsNoTracking(), "ID", "Name", selectedRegion);

           

            SqlCommand cmd = null;
            SqlConnection con = new System.Data.SqlClient.SqlConnection(DBConnection.Get());
            SqlDataReader reader = null;
            string query = "SELECT distinct CAST(KeyWord AS VARCHAR(MAX)) FROM KeyWords where KeyWord is not null";
            cmd = new SqlCommand(query);
            cmd.Connection = con;

            // string kwords = "";
            List<string> wordsList = new List<string>();
            con.Open();
            using (reader = cmd.ExecuteReader())
            {
                //  string[] kwords = new string[] { };
                //   int stx = 0;
                while (reader.Read())
                {
                    wordsList.Add(reader.GetString(0));
                    // read a row, for example:
                    //string keyW = reader.GetString(0);
                    //  kwords += reader.GetString(0).ToList();
                    //     kwords[stx] = reader.GetString(0);
                    //   stx++; 
                    // Console.WriteLine(keyW);
                }
                ViewBag.keyWordList = wordsList;
            }
            con.Close();
        }


        public IActionResult MySubscriptions(string favorite = null)
        {
            if (_user == null)
                _user = _userManager.GetUserAsync(User).Result;
            SetFavoriteDictionary.Set(_user.Id, favorite);
            return Ok("OK");
        }

        public IActionResult Newsletter(string id = null)
        {
            var model = new NewsletterViewModel(id);
            return View(model);
        }

        public IActionResult Subscribe(int ProviderID, int FieldOfInterestID)
        {
            if (_user == null)
                _user = _userManager.GetUserAsync(User).Result;

            var p1 = _db.Providers.Where(p => p.ID == ProviderID).FirstOrDefault();
            var f1 = _db.FieldsOfInterest.Where(c => c.ID == FieldOfInterestID).FirstOrDefault();

            if (p1 != null && f1 != null)
            {
                if (!_db.Subscriptions.Any(x => x.Subscriber == _user && x.Provider == p1 && x.FieldOfInterest == f1))
                {
                    var sub = new Subscription() { Subscriber = _user, Provider = p1, FieldOfInterest = f1, OptInStatus = 1 };

                    var matches = DBReader<string>.Read("select OptInLink from [dbo].[Campaigns] where PublisherID = @publisher and FieldOfInterestID = @interest", (r) =>
                    {
                        return r.IsDBNull(0) ? null : r.GetString(0);
                    }, (p) =>
                    {
                        p.Add(DBParameter.Int("publisher", ProviderID));
                        p.Add(DBParameter.Int("interest", FieldOfInterestID));
                    });
                    matches.Distinct().ToList().ForEach(m =>
                  {
                      LinksBag.Add(_user.Id, m);
                  });
                    var subLog = new SubscriptionsLog() { SubscriberID = _user.Id, ProviderID = Convert.ToString(p1.ID), CampaignID = Convert.ToString(f1.ID), OptIn = 1, DateModified = DateTime.Now };
                    sub.OptInStatus = 1;
                    sub.DateAdded = DateTime.Now;

                    //    _db.Subscriptions.Update(sub);
                    _db.Subscriptions.Add(sub);
                    _db.SubscriptionsLog.Add(subLog);

                    _db.SaveChanges();
                }
                else
                {
                    var sub = _db.Subscriptions
                   .Where(x => x.Subscriber == _user && x.Provider == p1 && x.FieldOfInterest == f1)
                   .FirstOrDefault();
                    sub.OptInStatus = 1;
                    sub.DateModified = DateTime.Now;
                    _db.Subscriptions.Attach(sub);
                    _db.Entry(sub).Property(x => x.OptInStatus).IsModified = true;
                    _db.Entry(sub).Property(x => x.DateModified).IsModified = true;

                    _db.Subscriptions.Update(sub);
                    var matches = DBReader<string>.Read("select OptInLink from [dbo].[Campaigns] where PublisherID = @publisher and FieldOfInterestID = @interest", (r) =>
                    {
                        return r.IsDBNull(0) ? null : r.GetString(0);
                    }, (p) =>
                    {
                        p.Add(DBParameter.Int("publisher", ProviderID));
                        p.Add(DBParameter.Int("interest", FieldOfInterestID));
                    });
                    matches.Distinct().ToList().ForEach(m =>
                    {
                        LinksBag.Add(_user.Id, m);
                    });
                    var subLog = new SubscriptionsLog() { SubscriberID = _user.Id, ProviderID = Convert.ToString(p1.ID), CampaignID = Convert.ToString(f1.ID), OptIn = 1, DateModified = DateTime.Now };
                    _db.SubscriptionsLog.Add(subLog);
                    //     _db.Subscriptions.Remove(subscription);
                    _db.SaveChanges();

                }
            }


            return RedirectToAction(nameof(MyContent));
        }

        public IActionResult Unsubscribe(int ProviderID, int FieldOfInterestID)
        {
            if (_user == null)
                _user = _userManager.GetUserAsync(User).Result;

            var p1 = _db.Providers.Where(p => p.ID == ProviderID).FirstOrDefault();
            var f1 = _db.FieldsOfInterest.Where(c => c.ID == FieldOfInterestID).FirstOrDefault();

            if (p1 != null && f1 != null)
            {
                var subscription = _db.Subscriptions
                    .Where(x => x.Subscriber == _user && x.Provider == p1 && x.FieldOfInterest == f1 && x.OptInStatus == 1)
                    .FirstOrDefault();

                if (subscription != null)
                {
                    var matches = DBReader<string>.Read("select OptOutLink from [dbo].[Campaigns] where PublisherID = @publisher and FieldOfInterestID = @interest", (r) =>
                    {
                        return r.IsDBNull(0) ? null : r.GetString(0);
                    }, (p) =>
                    {
                        p.Add(DBParameter.Int("publisher", ProviderID));
                        p.Add(DBParameter.Int("interest", FieldOfInterestID));
                    });
                    matches.Distinct().ToList().ForEach(m =>
                  {
                      LinksBag.Add(_user.Id, m);
                  });
                    var subLog = new SubscriptionsLog() { SubscriberID = _user.Id, ProviderID = Convert.ToString(p1.ID), CampaignID = Convert.ToString(f1.ID), OptIn = 0, DateModified = DateTime.Now };

                    subscription.OptInStatus = 0;
                    subscription.DateModified = DateTime.Now;
                    _db.Subscriptions.Attach(subscription);
                    _db.Entry(subscription).Property(x => x.OptInStatus).IsModified = true;
                    _db.Entry(subscription).Property(x => x.DateModified).IsModified = true;
                    _db.SubscriptionsLog.Add(subLog);

                    _db.Subscriptions.Update(subscription);
                    //     _db.Subscriptions.Remove(subscription);
                    _db.SaveChanges();
                }

            }

            return RedirectToAction(nameof(MyContent));
        }

        public IActionResult Achievements()
        {
            if (_user == null)
                _user = _userManager.GetUserAsync(User).Result;

            var viewModel = new AchievementsViewModel(_db, _userManager, _user);
            return View(viewModel);
        }

        public IActionResult Withdrawl()
        {
            if (_user == null)
                _user = _userManager.GetUserAsync(User).Result;

            var viewModel = new WithdrawlViewModel(_db, _userManager, _user);
            return View(viewModel);
        }

        [TempData]
        public string StatusMessage { get; set; }

        public IActionResult AccountSettings(bool? wasRedirected)
        {
            if (_user == null)
                _user = _userManager.GetUserAsync(User).Result;

            var vm = new AccountSettingsViewModel(_db, _userManager, _user);
            if (wasRedirected != null)
            {
                ModelState.AddModelError(string.Empty, "Invalid file type.");
                vm.StatusMessage = "uploadForm";
            }
            return View(vm);
        }
        [HttpPost]
        public ActionResult UpdateWallet(AccountSettingsViewModel vm)
        {
            if (_user == null)
                _user = _userManager.GetUserAsync(User).Result;

            if (_user == null)
            {
                throw new ApplicationException($"Unable to load user with ID '{_userManager.GetUserId(User)}'.");
            }

            if (!string.IsNullOrEmpty(vm.WalletAddress) && vm.WalletAddress.StartsWith("0x") && vm.WalletAddress.Length == 42)
            {
                _user.WalletAddress = vm.WalletAddress;
                _db.SaveChanges();
                return Ok("Wallet address changed.");
            }
            else
            {
                return Ok("Wallet address is not valid!");
            }
        }

        [HttpPost]
        public async Task<IActionResult> AccountSettings(AccountSettingsViewModel vm)
        {
            vm.StatusMessage = "inputForm";

            if (_user == null)
                _user = _userManager.GetUserAsync(User).Result;

            if (_user == null)
            {
                throw new ApplicationException($"Unable to load user with ID '{_userManager.GetUserId(User)}'.");
            }
            vm.UserImage = _user.Id + ".jpg";
            if (!ModelState.IsValid)
            {
                ModelState.AddModelError(string.Empty, "Some error occured.");
                return View(vm);
            }

            if (string.IsNullOrEmpty(vm.OldPassword))
            {
                ModelState.AddModelError(string.Empty, "Current password cannot be null.");
                return View(vm);
            }
            if (string.IsNullOrEmpty(vm.NewPassword))
            {
                ModelState.AddModelError(string.Empty, "New Password cannot be null.");
                return View(vm);
            }
            if (string.IsNullOrEmpty(vm.ConfirmPassword))
            {
                ModelState.AddModelError(string.Empty, "Confirm Password cannot be null.");
                return View(vm);
            }
            if (!string.IsNullOrEmpty(vm.NewPassword) && !string.IsNullOrEmpty(vm.NewPassword) && !string.IsNullOrEmpty(vm.ConfirmPassword))
            {
                var changePasswordResult = await _userManager.ChangePasswordAsync(_user, vm.OldPassword, vm.NewPassword);
                if (!changePasswordResult.Succeeded)
                {
                    AddErrors(changePasswordResult);
                    return View(vm);
                }

                await _signInManager.SignInAsync(_user, isPersistent: false);
                _logger.LogInformation("User changed their password successfully.");
                //StatusMessage = "Your password has been changed.";
                ModelState.AddModelError(string.Empty, "You have changed your password successfully.");
                return View(vm);
            }

            return RedirectToAction(nameof(AccountSettings));
        }
        private void AddErrors(IdentityResult result)
        {
            foreach (var error in result.Errors)
            {
                ModelState.AddModelError(string.Empty, error.Description);
            }
        }

        public IActionResult listWords()
        {


            return Json(_db.KeyWords.Include(x => x.campaignsset).ToList());
        }

        [HttpGet]
        public IActionResult searchResponse()
        {



            SqlCommand cmd = null;
            SqlConnection con = new System.Data.SqlClient.SqlConnection(DBConnection.Get());
            SqlDataReader rdr = null;
            string allData = null;
            var Objectx = new List<string>();
            // string[] strArray = new string[];
            string x = null;
            try
            {

                string query = ("SELECT k.KeyWord as keyword FROM  KeyWords k FOR JSON PATH");

                cmd = new SqlCommand(query);
                cmd.Connection = con;

                // Add LastName to the above defined paramter @Find
                //cmd.Parameters.Add(
                //    new SqlParameter(
                //    "@sr", // The name of the parameter to map
                //   phrase));  // The name of the source column

                //// Fill the parameter with the value retrieved
                //// from the text field
                //cmd.Parameters["@sr"].Value = phrase;
                con.Open();

                // Execute the query
                rdr = cmd.ExecuteReader();

                while (rdr.Read())
                {
                    var tmp = rdr.GetString(0);

                    Objectx.Add(tmp.ToString());
                    x += rdr.GetString(0);
                }

            }
            catch (Exception ex)
            {
                throw (ex);
                // Print error message
                //  Console.log(ex.Message);
            }
            finally
            {
                // Close data reader object and database connection
                if (rdr != null)
                    rdr.Close();

                if (con.State == ConnectionState.Open)
                    con.Close();
            }

            Console.Write(Json(Objectx));

            return Json(Objectx);

        }
        [HttpGet("word")]
        public IActionResult getCampaignIds(string word)
        {

            SqlCommand cmd = null;
            SqlConnection con = new System.Data.SqlClient.SqlConnection(DBConnection.Get());
            SqlDataReader rdr = null;
            string allData = null;
            var Objectx = new List<string>();
            // string[] strArray = new string[];
            string x = null;
            try
            {

                string query = ("SELECT c.ID FROM Campaigns c INNER JOIN CampaignsHasKeywords chk ON c.ID = chk.CampaignID INNER JOIN KeyWords k ON k.ID = chk.KeyWordID WHERE k.KeyWord LIKE '%' + @word + '%'  FOR JSON PATH");

                cmd = new SqlCommand(query);
                cmd.Connection = con;

              
                cmd.Parameters.Add(
                    new SqlParameter(
                    "@word", // The name of the parameter to map
                   word
                   ));  // The name of the source column

                // Fill the parameter with the value retrieved
                // from the text field
                cmd.Parameters["@word"].Value = word;
                con.Open();

                // Execute the query
                rdr = cmd.ExecuteReader();

                while (rdr.Read())
                {
                    var tmp = rdr.GetString(0);

                    Objectx.Add(tmp.ToString());
                    Console.Write(tmp); 
                  
                }

            }
            catch (Exception ex)
            {
                throw (ex);
                // Print error message
                //  Console.log(ex.Message);
            }
            finally
            {
                // Close data reader object and database connection
                if (rdr != null)
                    rdr.Close();

                if (con.State == ConnectionState.Open)
                    con.Close();
            }



            Console.Write(Json(Objectx));

            return Json(Objectx);





        }
    }
}

