using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using Emmares4.Data;
using Emmares4.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Emmares4.Models.HomeViewModels;
using Microsoft.Extensions.Logging;
using System.Diagnostics;
using Emmares4.Models.ManageViewModels;
using Microsoft.AspNetCore.Http;
using System.IO;
using System.Net.Http.Headers;
using System.Data.Common;
using Dapper;
using Emmares4.Helpers;
using System.Data.SqlClient;
using System.Data;
using Microsoft.AspNetCore.Hosting;
using System.Net;
using System.Xml.Linq;
using Emmares4.Elastic;

using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Processing;
using SixLabors.ImageSharp.Processing.Transforms;

namespace Emmares4.Controllers
{
    [Authorize(Roles = "Marketeer")]
    [Route("[controller]/[action]")]
    public class CampaignsController : Controller
    {
        private readonly SignInManager<ApplicationUser> _signInManager;
        private readonly ILogger _logger;
        ApplicationDbContext _db;
        UserManager<ApplicationUser> _userManager;
        ApplicationUser _user;
        DbConnection _dbConnection;
        private readonly IHostingEnvironment _hostingEnvironment;

        public CampaignsController(ApplicationDbContext context, UserManager<ApplicationUser> userManager,
            DbConnection dbConnection,
            ILogger<HomeController> logger, SignInManager<ApplicationUser> signInManager, IHostingEnvironment hostingEnvironment)
        {
            _db = context;
            _userManager = userManager;
            _logger = logger;
            _signInManager = signInManager;
            _dbConnection = dbConnection;
            _hostingEnvironment = hostingEnvironment;
        }

        public IActionResult Check(string url)
        {
            var wc = new WebClient();
            try
            {
                var res = wc.DownloadString(url);
                return Ok("Valid");
            }
            catch (Exception)
            {
                return Ok("Invalid");
            }
        }

        // GET: Campaigns
        public async Task<IActionResult> Index()
        {
            if (_user == null)
                _user = _userManager.GetUserAsync(User).Result;
            if (_user == null)
                return RedirectToAction("Login", "Account");
            var query = @"SELECT c.Id, c.Name, f.Name AS FieldOfInterest, r.Name AS Region, cnt.Name AS ContentType, c.Recipients,
                                    isnull (c.Budget + (select isnull(sum (Reward), 0) from [Statistics] s where s.CampaignID = c.ID), 0) AS Budget,
                                    c.Budget AS Remaining, CAST(c.DateAdded as date) AS AddedOn, COUNT(ch.CampaignID) as Hits
                            FROM Campaigns c
                            INNER JOIN ContentTypes cnt on c.ContentTypeID = cnt.ID
                            INNER JOIN FieldsOfInterest f on c.FieldOfInterestID = f.ID
                            INNER JOIN Regions r on c.RegionID = r.ID
                            INNER JOIN Providers p on p.ID = c.PublisherID
                            LEFT OUTER JOIN CampaignsHits ch on c.ID = ch.CampaignID
                            WHERE p.OwnerId = @user
                            GROUP BY c.Id, c.Name, cnt.Name , r.Name , f.Name , c.Recipients, c.Budget, c.DateAdded, ch.CampaignID
                            ORDER BY c.DateAdded;";

            var camp = await _dbConnection.QueryAsync<CampaignViewModel>(query, new { user = _user.Id });

            if (camp == null)
            {
                return RedirectToAction(nameof(AccountSettings));
            } else
            {
                return View(camp.ToList());
            }

            
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

        //



        //


        // GET: Campaigns/Details/5
        public async Task<IActionResult> Details(Guid? id)
        {
            SetBaseUrl();

            if (id == null)
            {
                return NotFound();
            }
            if (_user == null)
                _user = _userManager.GetUserAsync(User).Result;


            var campaign = await _db.Campaigns
                .Include(c => c.Publisher)
                .SingleOrDefaultAsync(m => m.ID == id);
            // var inputKeyWords = _db.KeyWords
            //     campaign.inputKeyWords = DBReader<string>.Read("SELECT KeyWord FROM KeyWords kw INNER JOIN CampaignsHasKeywords chk ON chk.KeyWordID = kw.ID INNER JOIN Campaigns c ON c.ID = chk.CampaignID WHERE c.ID LIKE @CID;", (r) =>

            PopulateDropDownList();
            campaign.AvailableBalance = _user.Balance;


            if (campaign == null)
            {
                return NotFound();
            }
            SqlCommand cmd = null;
            SqlConnection con = new System.Data.SqlClient.SqlConnection(DBConnection.Get());
            SqlDataReader reader = null;
            string query = "SELECT KeyWord FROM KeyWords kw INNER JOIN CampaignsHasKeywords chk ON chk.KeyWordID = kw.ID INNER JOIN Campaigns c ON c.ID = chk.CampaignID WHERE c.ID LIKE @CID;";
            cmd = new SqlCommand(query);
            cmd.Connection = con;
            cmd.Parameters.Add(
                    new SqlParameter(
                    "@CID", // The name of the parameter to map
                   id));
            string kwords = "";
            con.Open();
            using (reader = cmd.ExecuteReader())
            {
                while (reader.Read())
                {
                    // read a row, for example:
                    //string keyW = reader.GetString(0);
                    kwords += reader.GetString(0) + " ";
                    // Console.WriteLine(keyW);
                }
                campaign.inputKeyWords = kwords;
            }
            con.Close();

            return View(campaign);
        }

        public IActionResult GeoInformation()
        {
            if (_user == null)
                _user = _userManager.GetUserAsync(User).Result;
            var model = new GeoInformationViewModel(_user.Id);
            return View(model);
        }


        [HttpPost]
        public IActionResult GeoInformation(GeoInformationViewModel viewModel)
        {
            if (!ModelState.IsValid)
                return View(viewModel);

            viewModel.CitiesCount.Clear();
            foreach (var i in viewModel.GeoInformation) // 0=CampaignName, 1=City, 2=Country, 3=Date
            {
                if (i[0] == viewModel.SelectedCampaign &&
                    DateTime.Parse(i[3]) > DateTime.Parse(viewModel.SelectedStartDate) &&
                    DateTime.Parse(i[3]) < DateTime.Parse(viewModel.SelectedEndDate))
                {
                    if (viewModel.CitiesCount.ContainsKey(i[0]))
                    {
                        viewModel.CitiesCount[i[0]] = viewModel.CitiesCount[i[0]] + 1;  // Increment current value in the Dictionary
                    }
                    else
                    {
                        viewModel.CitiesCount.Add(i[0], 1);                         // Add new entry
                    }
                }
            }
            return Redirect("Campaigns/GeoInformation");
        }

        private void SetBaseUrl()
        {
            if (string.IsNullOrWhiteSpace(Paths.BaseUrl)) { Paths.BaseUrl = Request.Scheme + "://" + Request.Host + Request.PathBase + "/"; }
        }

        private void PopulateDropDownList(object selectedContentType = null, object selectedFieldOfInterest = null, object selectedRegion = null)
        {
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
        }

        public IActionResult Analytics()
        {
            if (_user == null)
                _user = _userManager.GetUserAsync(User).Result;

            var viewModel = new AnalyticsViewModel(_db, _dbConnection, _userManager, _user, ChartInterval.Week);
            return View(viewModel);
        }

        public IActionResult GetChart(int chartType, string view, string sql_file_data, string sql_file_stats)
        {
            if (_user == null)
                _user = _userManager.GetUserAsync(User).Result;
            var viewModel = new ChartViewModel(_db, _dbConnection, _user, (ChartInterval)chartType, sql_file_data, sql_file_stats);
            return PartialView(view, viewModel);
        }

        // GET: Campaigns/FakeEMAs
        public IActionResult FakeEMAs()
        {
            if (_user == null)
                _user = _userManager.GetUserAsync(User).Result;

            _user.Balance += 100;
            _db.SaveChanges();
            return Ok("Increased");
        }

        // GET: Campaigns/Create
        public IActionResult Create()
        {
            SetBaseUrl();

            if (_user == null)
                _user = _userManager.GetUserAsync(User).Result;



            PopulateDropDownList();

            Campaign c = new Campaign();

            c.AvailableBalance = _user.Balance;
            _db.Providers.Include(pc => pc.Owner).Load();

            c.Publisher = _db.Providers.FirstOrDefault(x => x.Owner == _user);

            return View(c);
        }


        // POST: Campaigns/Create
        // To protect from overposting attacks, please enable the specific properties you want to bind to, for 
        // more details see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Campaign campaign)
        {
            SetBaseUrl();
            PopulateDropDownList();
            if (ModelState.IsValid)
            {
                if (_user == null)
                    _user = _userManager.GetUserAsync(User).Result;

                if (campaign.Budget > campaign.AvailableBalance)
                {
                    PopulateDropDownList();
                    ModelState.AddModelError(string.Empty, "You dont have enough balance to create this campaign.");
                    return View(campaign);
                }
                string tmp = campaign.inputKeyWords;
                char delimiter = ',';
                string[] words = tmp.Split(delimiter);


                campaign.DateAdded = DateTime.UtcNow;
                campaign.DateModified = DateTime.UtcNow;

                campaign.Publisher = _db.Providers.Where(p => p.Owner == _user).FirstOrDefault();

                _db.Add(campaign);
                _user.Balance = _user.Balance - campaign.Budget;
                _db.SaveChanges();
                await _db.SaveChangesAsync();
                for (int co = 0; co < words.Length; co++)
                {
                    var kw = new KeyWords { KeyWord = words[co] };
                    _db.Add(kw);
                    _db.SaveChanges();
                    var kwhc = new CampaignsHasKeywords { campaignset = campaign, keyWordsset = kw };
                    _db.Add(kwhc);
                    _db.SaveChanges();

                    _db.SaveChanges();
                }
                return RedirectToAction("Details", new { id = campaign.ID.ToString() });
            }
            return View(campaign);
        }

        public void SaveNewsletter(Campaign campaign)
        {
            // Deprecated
            XDocument doc = new XDocument(
                        new XDocumentType("html", null, null, null),
                        new XElement("html",
                            new XElement("style", new XAttribute("type", "text/css"), "body { margin: 10; padding: 10; background-color: blue;}"),
                            new XElement("body",
                                new XElement("h1", campaign.Name),
                                new XElement("p", "Published by " + campaign.Publisher + " in field " + campaign.FieldOfInterestID),
                                new XElement("p", "Region:\t" + campaign.RegionID),
                                new XElement("p", "Content Type:\t" + campaign.ContentTypeID),
                                new XElement("p", "Recipients:\t" + campaign.Recipients),
                                new XElement("p", "Budget:\t" + campaign.Budget),
                                new XElement("p", "Keywords:\t" + campaign.inputKeyWords),
                                new XElement("p", "General descritpion: Some text")
                                )
                        )
                    );
            Directory.CreateDirectory("wwwroot/newsletters");
            doc.Save("wwwroot/newsletters/" + campaign.Name.Replace(' ', '_').ToLower() + ".html");
        }

        // GET: Campaigns/Edit/5
        public async Task<IActionResult> Edit(Guid? id)
        {
            SetBaseUrl();

            if (id == null)
            {
                return NotFound();
            }

            var campaign = await _db.Campaigns.SingleOrDefaultAsync(m => m.ID == id);
            if (campaign == null)
            {
                return NotFound();
            }
            return View(campaign);
        }

        // POST: Campaigns/Edit/5
        // To protect from overposting attacks, please enable the specific properties you want to bind to, for 
        // more details see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(Guid id, [Bind("ID,Name,PublisherID,Recipients,Budget,FieldOfInterestID,RegionID,ContentTypeID,OptInLink,OptOutLink")] Campaign campaign)
        {
            SetBaseUrl();

            if (id != campaign.ID)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    // ti dve polji je potrebno ponastaviti, ker se ne preneseta iz forme in ju drugace EF nulira
                    campaign.DateModified = DateTime.UtcNow;
                    campaign.DateAdded = DBReader<DateTime>.Read("select DateAdded from [dbo].[Campaigns] where ID = @id", (r) =>
                       {
                           return r.IsDBNull(0) ? DateTime.UtcNow : r.GetDateTime(0);
                       }, (p) =>
                       {
                           p.Add(DBParameter.String("id", campaign.ID.ToString()));
                       }).First();

                    _db.Update(campaign);
                    await _db.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!CampaignExists(campaign.ID))
                    {
                        return NotFound();
                    }
                    else
                    {
                        throw;
                    }
                }
                return RedirectToAction(nameof(Index));
            }
            return View(campaign);
        }

        // GET: Campaigns/Delete/5
        public async Task<IActionResult> Delete(Guid? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var campaign = await _db.Campaigns
                .SingleOrDefaultAsync(m => m.ID == id);
            if (campaign == null)
            {
                return NotFound();
            }

            return View(campaign);
        }
        private string GetContentType(string path)
        {
            var types = GetMimeTypes();
            var ext = Path.GetExtension(path).ToLowerInvariant();
            return types[ext];
        }

        private Dictionary<string, string> GetMimeTypes()
        {
            return new Dictionary<string, string>
            {
                {".txt", "text/plain"},
                {".pdf", "application/pdf"},
                {".doc", "application/vnd.ms-word"},
                {".docx", "application/vnd.ms-word"},
                {".xls", "application/vnd.ms-excel"},
                {".png", "image/png"},
                {".jpg", "image/jpeg"},
                {".jpeg", "image/jpeg"},
                {".gif", "image/gif"},
                {".csv", "text/csv"}
            };
        }


        //tadej csv export
        [HttpPost]
        public async Task<IActionResult> Export(Guid id)
        {
            SqlCommand cmd = null;
            SqlConnection con = new System.Data.SqlClient.SqlConnection(DBConnection.Get());
            SqlDataReader rdr = null;
            string allData = null;
            try
            {

                string query = ("SELECT u.email, s.DateAdded, s.OptInStatus FROM AspNetUsers u INNER JOIN Subscriptions s ON s.SubscriberId = u.Id WHERE s.ProviderID in (SELECT s.ProviderID FROM Subscriptions s INNER JOIN Providers p ON s.ProviderID = p.ID INNER JOIN Campaigns c ON p.ID = c.PublisherID WHERE c.ID LIKE @ID) ");
                cmd = new SqlCommand(query);
                cmd.Connection = con;

                // Add LastName to the above defined paramter @Find
                cmd.Parameters.Add(
                    new SqlParameter(
                    "@ID", // The name of the parameter to map
                   id));  // The name of the source column

                // Fill the parameter with the value retrieved
                // from the text field
                cmd.Parameters["@ID"].Value = id;
                con.Open();

                // Execute the query
                rdr = cmd.ExecuteReader();

                var csv = new System.Text.StringBuilder();

                //in your loop
                var path = Path.Combine(_hostingEnvironment.ContentRootPath, "App_Data/Exports");
                var filepath = Path.Combine(path, id.ToString() + ".csv");
                if (!Directory.Exists(path)) { Directory.CreateDirectory(path); }



                // var filepath = "D:\myOutput.csv";

                //  _db.Update(campaign);
                var header = string.Format("{0}", "Email" + "," + "Date added" + "," + "Opt in status");
                csv.AppendLine(header);
              
                while (rdr.Read())
                {
                    var newLine = string.Format("{0}", (rdr["Email"].ToString()) + "," + (rdr["DateAdded"].ToString()) + "," + (rdr["OptInStatus"].ToString()));
                    csv.AppendLine(newLine);
                }
                System.IO.File.WriteAllText(filepath, csv.ToString());

                var memory = new MemoryStream();
                using (var stream = new FileStream(filepath, FileMode.Open))
                {
                    await stream.CopyToAsync(memory);
                }
                memory.Position = 0;
                return File(memory, GetContentType(filepath), Path.GetFileName(filepath));
            }
            catch (Exception ex)
            {
                throw (ex);

            }
            finally
            {
                // Close data reader object and database connection
                if (rdr != null)
                    rdr.Close();

                if (con.State == ConnectionState.Open)
                    con.Close();
            }
        }

        // POST: Campaigns/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(Guid id)
        {
            SqlCommand cmd = null;
            SqlConnection con = new System.Data.SqlClient.SqlConnection(DBConnection.Get());
            SqlDataReader reader = null;
            string query = "DELETE FROM CampaignsHasKeywords WHERE CampaignID IN (SELECT ID FROM dbo.Campaigns WHERE ID LIKE @id);";
            string query2 = "DELETE FROM KeyWords WHERE ID IN (SELECT k.KeyWordID FROM CampaignsHasKeywords k WHERE CampaignID IN (SELECT ID FROM dbo.Campaigns WHERE ID LIKE @id));";
            string query3 = "DELETE FROM dbo.[Statistics] WHERE CampaignID IN (SELECT ID FROM dbo.Campaigns WHERE ID LIKE @id);";
            string query4 = "DELETE FROM CampaignsHits WHERE CampaignID IN (SELECT ID FROM dbo.Campaigns WHERE ID LIKE @id);";
            string query5 = "DELETE FROM dbo.Campaigns WHERE ID LIKE @id;";
            cmd = new SqlCommand(query);
            cmd.Connection = con;
            cmd.Parameters.Add(
                    new SqlParameter(
                    "@ID", // The name of the parameter to map
                   id));
            con.Open();
            cmd.ExecuteNonQuery();

            cmd = new SqlCommand(query2);
            cmd.Connection = con;
            cmd.Parameters.Add(
                    new SqlParameter(
                    "@ID", // The name of the parameter to map
                   id));
            cmd.ExecuteNonQuery();

            cmd = new SqlCommand(query3);
            cmd.Connection = con;
            cmd.Parameters.Add(
                    new SqlParameter(
                    "@ID", // The name of the parameter to map
                   id));
            cmd.ExecuteNonQuery();
            StatisticsES.DeleteCampaignStats(id.ToString().ToLower());

            cmd = new SqlCommand(query4);
            cmd.Connection = con;
            cmd.Parameters.Add(
                    new SqlParameter(
                    "@ID", // The name of the parameter to map
                   id));
            cmd.ExecuteNonQuery();

            cmd = new SqlCommand(query5);
            cmd.Connection = con;
            cmd.Parameters.Add(
                    new SqlParameter(
                    "@ID", // The name of the parameter to map
                   id));
            cmd.ExecuteNonQuery();

            // var campaign = await _db.Campaigns.SingleOrDefaultAsync(m => m.ID == id);
            //_db.Campaigns.Remove(campaign);
            //wait _db.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        private bool CampaignExists(Guid id)
        {
            return _db.Campaigns.Any(e => e.ID == id);
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
            var publisher = _db.Providers.Where(x => x.Owner == _user).FirstOrDefault();
            {
                if (publisher == null)
                    _db.Providers.Add(new Provider() { Name = vm.PublisherName, Owner = _user, DateAdded = DateTime.UtcNow });
                else if (!string.IsNullOrEmpty(vm.PublisherName))
                    publisher.Name = vm.PublisherName;
                _db.SaveChanges();
            }
            if (!ModelState.IsValid)
            {
                ModelState.AddModelError(string.Empty, "Some error occured.");
                return View(vm);
            }

            //if (string.IsNullOrEmpty(vm.OldPassword))
            //{
            //    ModelState.AddModelError(string.Empty, "Current password cannot be null.");
            //    return View(vm);
            //}
            //if (string.IsNullOrEmpty(vm.NewPassword))
            //{
            //    ModelState.AddModelError(string.Empty, "New Password cannot be null.");
            //    return View(vm);
            //}
            //if (string.IsNullOrEmpty(vm.ConfirmPassword))
            //{
            //    ModelState.AddModelError(string.Empty, "Confirm Password cannot be null.");
            //    return View(vm);
            //}

            if ((string.IsNullOrEmpty(vm.OldPassword)) && (!string.IsNullOrEmpty(vm.NewPassword)) && (!string.IsNullOrEmpty(vm.ConfirmPassword)))
            {
                ModelState.AddModelError(string.Empty, "Current password cannot be null.");
                return View(vm);
            }

            if ((!string.IsNullOrEmpty(vm.OldPassword)) && (!string.IsNullOrEmpty(vm.NewPassword)) && (string.IsNullOrEmpty(vm.ConfirmPassword)))
            {
                ModelState.AddModelError(string.Empty, "Confirm Password cannot be null.");
                return View(vm); 
            }

            if ((string.IsNullOrEmpty(vm.OldPassword)) && (string.IsNullOrEmpty(vm.NewPassword)) && (string.IsNullOrEmpty(vm.ConfirmPassword)))
            {
            }
            else {

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

        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }

    }
}