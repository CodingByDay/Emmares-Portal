using System;
using System.Diagnostics;

using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Diagnostics;

using Emmares4.Models;
using Emmares4.Helpers;

namespace Emmares4.Controllers
{
    public class ErrorController : Controller
    {
        private readonly IHostingEnvironment _hostingEnvironment;
        private readonly UserManager<ApplicationUser> _userManager;

        public ErrorController(IHostingEnvironment hostingEnvironment, UserManager<ApplicationUser> userManager)
        {
            _hostingEnvironment = hostingEnvironment;
            _userManager = userManager;
            Log.Write("ErrorController started");
        }

        public IActionResult Report()
        {
            Log.Write("ErrorController Report start");
            var rqID = "unknown";
            var timestamp = DateTime.Now.ToString("s");
            try
            {
                try { rqID = Activity.Current?.Id ?? HttpContext.TraceIdentifier; } catch (Exception rex) { rqID += "/" + rex.Message; }

                var _user = _userManager.GetUserAsync(User).Result;

                var ex = HttpContext.Features.Get<IExceptionHandlerFeature>();
                var message = "unknown";
                try { message = ex.Error.Message; } catch (Exception mex) { message += "/" + mex.Message; }                

                try
                {
                    var report =
                        "  Request: " + rqID + " @ " + _user?.Id + Environment.NewLine +
                        "  Message: " + message + Environment.NewLine +
                        "  Timestamp: " + timestamp + Environment.NewLine + Environment.NewLine +
                        "  " + ex.Error.ToString() + Environment.NewLine;
                    Log.Write("ErrorController Report: " + Environment.NewLine + report);
                    return View(new ErrorViewModel { RequestId = rqID + " @ " + _user?.Id, Message = message, Timestamp = timestamp });
                }
                catch (Exception eex)
                {
                    Log.Write("ErrorControler Report failed: " + eex.ToString());
                    return View(new ErrorViewModel { RequestId = rqID + " @ " + _user?.Id, Message = message + " --- " + eex.ToString(), Timestamp = timestamp });
                }
            }
            catch (Exception ex)
            {
                Log.Write("ErrorControler Report failed: " + ex.ToString());
                return View(new ErrorViewModel { RequestId = rqID, Message = ex.Message + " --- " + ex.ToString(), Timestamp = timestamp });
            }
        }
    }
}