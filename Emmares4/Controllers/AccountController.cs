using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Emmares4.Models;
using Emmares4.Models.AccountViewModels;
using Emmares4.Services;
using Emmares4.Data;
using Microsoft.AspNetCore.Http;
using System.Data.Common;
using System.Net.Mail;
using Microsoft.Extensions.Caching.Memory;

using Emmares4.Helpers;
using Emmares4.Elastic;

namespace Emmares4.Controllers
{
    [Authorize]
    [Route("[controller]/[action]")]
    public class AccountController : Controller
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly SignInManager<ApplicationUser> _signInManager;
        private readonly RoleManager<IdentityRole> _roleManager;
        private readonly IEmailSender _emailSender;
        private readonly ILogger _logger;
        private readonly ApplicationDbContext _db;
        private IMemoryCache _cache;
        public AccountController(
            ApplicationDbContext context,
            UserManager<ApplicationUser> userManager,
            SignInManager<ApplicationUser> signInManager,
            RoleManager<IdentityRole> roleManager,
            IEmailSender emailSender,
            ILogger<AccountController> logger,
            IMemoryCache memoryCache)
        {
            _db = context;
            _roleManager = roleManager;
            _userManager = userManager;
            _signInManager = signInManager;
            _emailSender = emailSender;
            _logger = logger;
            _cache = memoryCache;
        }

        [TempData]
        public string ErrorMessage { get; set; }

        [HttpGet]
        [AllowAnonymous]
        public async Task<IActionResult> Login(string returnUrl = null)
        {
            // Clear the existing external cookie to ensure a clean login process
            await HttpContext.SignOutAsync(IdentityConstants.ExternalScheme);

            ViewData["ReturnUrl"] = returnUrl;
            return View();
        }

        [HttpPost]
        [AllowAnonymous]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Login(LoginViewModel model, string returnUrl = null)
        {
            ViewData["ReturnUrl"] = returnUrl;
            if (ModelState.IsValid)
            {
                // This doesn't count login failures towards account lockout
                // To enable password failures to trigger account lockout, set lockoutOnFailure: true

                var currentRole = "Marketeer";

                var u = await _userManager.FindByEmailAsync(model.Email);

                if (u == null)
                {
                    ModelState.AddModelError(string.Empty, "Username or password is incorrect");
                    return View(model);
                }

                bool isMarketeer = _userManager.GetRolesAsync(u).Result.Any(x => x.Equals(currentRole, StringComparison.InvariantCultureIgnoreCase));

                //if (!isMarketeer)
                //{
                //    ModelState.AddModelError(string.Empty, "you are not in correct role");
                //    return View(model);
                //}

                var result = await _signInManager.PasswordSignInAsync(model.Email, model.Password, model.RememberMe, lockoutOnFailure: false);

                if (result.Succeeded)
                {
                    _logger.LogInformation("User logged in.");

                    if (!string.IsNullOrWhiteSpace (returnUrl))
                    {
                        return RedirectToLocal(returnUrl);
                    }

                    if (isMarketeer)
                    {
                        return RedirectToAction("Index", "Campaigns");
                    }
                    else
                    {
                        return RedirectToLocal("/Home/Dashboard");
                    }
                }
                if (result.RequiresTwoFactor)
                {
                    return RedirectToAction(nameof(LoginWith2fa), new { returnUrl, model.RememberMe });
                }
                if (result.IsLockedOut)
                {
                    _logger.LogWarning("User account locked out.");
                    return RedirectToAction(nameof(Lockout));
                }
                else
                {
                    ModelState.AddModelError(string.Empty, "Invalid login attempt.");
                    return View(model);
                }
            }

            // If we got this far, something failed, redisplay form
            return View(model);
        }

        [HttpGet]
        [AllowAnonymous]
        public async Task<IActionResult> LoginWith2fa(bool rememberMe, string returnUrl = null)
        {
            // Ensure the user has gone through the username & password screen first
            var user = await _signInManager.GetTwoFactorAuthenticationUserAsync();

            if (user == null)
            {
                throw new ApplicationException($"Unable to load two-factor authentication user.");
            }

            var model = new LoginWith2faViewModel { RememberMe = rememberMe };
            ViewData["ReturnUrl"] = returnUrl;

            return View(model);
        }

        [HttpPost]
        [AllowAnonymous]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> LoginWith2fa(LoginWith2faViewModel model, bool rememberMe, string returnUrl = null)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            var user = await _signInManager.GetTwoFactorAuthenticationUserAsync();
            if (user == null)
            {
                throw new ApplicationException($"Unable to load user with ID '{_userManager.GetUserId(User)}'.");
            }

            var authenticatorCode = model.TwoFactorCode.Replace(" ", string.Empty).Replace("-", string.Empty);

            var result = await _signInManager.TwoFactorAuthenticatorSignInAsync(authenticatorCode, rememberMe, model.RememberMachine);

            if (result.Succeeded)
            {
                _logger.LogInformation("User with ID {UserId} logged in with 2fa.", user.Id);
                return RedirectToLocal(returnUrl);
            }
            else if (result.IsLockedOut)
            {
                _logger.LogWarning("User with ID {UserId} account locked out.", user.Id);
                return RedirectToAction(nameof(Lockout));
            }
            else
            {
                _logger.LogWarning("Invalid authenticator code entered for user with ID {UserId}.", user.Id);
                ModelState.AddModelError(string.Empty, "Invalid authenticator code.");
                return View();
            }
        }

        [HttpGet]
        [AllowAnonymous]
        public async Task<IActionResult> LoginWithRecoveryCode(string returnUrl = null)
        {
            // Ensure the user has gone through the username & password screen first
            var user = await _signInManager.GetTwoFactorAuthenticationUserAsync();
            if (user == null)
            {
                throw new ApplicationException($"Unable to load two-factor authentication user.");
            }

            ViewData["ReturnUrl"] = returnUrl;

            return View();
        }

        [HttpPost]
        [AllowAnonymous]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> LoginWithRecoveryCode(LoginWithRecoveryCodeViewModel model, string returnUrl = null)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            var user = await _signInManager.GetTwoFactorAuthenticationUserAsync();
            if (user == null)
            {
                throw new ApplicationException($"Unable to load two-factor authentication user.");
            }

            var recoveryCode = model.RecoveryCode.Replace(" ", string.Empty);

            var result = await _signInManager.TwoFactorRecoveryCodeSignInAsync(recoveryCode);

            if (result.Succeeded)
            {
                _logger.LogInformation("User with ID {UserId} logged in with a recovery code.", user.Id);
                return RedirectToLocal(returnUrl);
            }
            if (result.IsLockedOut)
            {
                _logger.LogWarning("User with ID {UserId} account locked out.", user.Id);
                return RedirectToAction(nameof(Lockout));
            }
            else
            {
                _logger.LogWarning("Invalid recovery code entered for user with ID {UserId}", user.Id);
                ModelState.AddModelError(string.Empty, "Invalid recovery code entered.");
                return View();
            }
        }

        [HttpGet]
        [AllowAnonymous]
        public IActionResult Lockout()
        {
            return View();
        }

        [HttpGet]
        [AllowAnonymous]
        public IActionResult Register(string returnUrl = null)
        {
            ViewData["ReturnUrl"] = returnUrl;
            return View();
        }

        [HttpPost]
        [AllowAnonymous]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Register(RegisterViewModel model, string returnUrl = null)
        {
            ViewData["ReturnUrl"] = returnUrl;

            if (ModelState.IsValid)
            {
                string firstName = string.Empty, lastName = string.Empty; //, provider = string.Empty;
                firstName = model.FirstName;
                lastName = model.LastName;
               // provider = model.Provider;

               

                var user = new ApplicationUser { UserName = model.Email, FirstName = firstName, LastName = lastName, Email = model.Email };

                var roleName = model.IsMarketeer ? "Marketeer" : "Recipient";
                roleName = "Marketeer"; //Marketeer is default
                bool exist = await _roleManager.RoleExistsAsync(roleName);
                if (!exist)
                {
                    var role = new IdentityRole
                    {
                        Name = roleName
                    };
                    await _roleManager.CreateAsync(role);
                }

                var result = await _userManager.CreateAsync(user, model.Password);

                if (result.Succeeded)
                {
                    _logger.LogInformation("User created a new account with password.");

                    var re = await _userManager.AddToRoleAsync(user, roleName);

                    var code = await _userManager.GenerateEmailConfirmationTokenAsync(user);
                    var callbackUrl = Url.EmailConfirmationLink(user.Id, code, Request.Scheme);
                    await _emailSender.SendEmailConfirmationAsync(model.Email, callbackUrl);

                    await _signInManager.SignInAsync(user, isPersistent: false);
                    _logger.LogInformation("User created a new account with password.");

                    if (model.IsMarketeer)
                    {
                        await _userManager.AddToRoleAsync(user, "Recipient");

                        return RedirectToAction("Index", "Campaigns");
                    }
                    return RedirectToLocal(returnUrl);

                }

                AddErrors(result);

            }

            // If we got this far, something failed, redisplay form
            return View(model);
        }


        public async Task<IActionResult> Logout()
        {
            await _signInManager.SignOutAsync();
            _logger.LogInformation("User logged out.");
            return RedirectToAction(nameof(HomeController.Dashboard), "Home");
        }

        [HttpPost]
        [AllowAnonymous]
        [ValidateAntiForgeryToken]
        public IActionResult ExternalLogin(string provider, string returnUrl = null)
        {
            // Request a redirect to the external login provider.
            var redirectUrl = Url.Action(nameof(ExternalLoginCallback), "Account", new { returnUrl });
            var properties = _signInManager.ConfigureExternalAuthenticationProperties(provider, redirectUrl);
            return Challenge(properties, provider);
        }

        [HttpGet]
        [AllowAnonymous]
        public async Task<IActionResult> ExternalLoginCallback(string returnUrl = null, string remoteError = null)
        {
            if (remoteError != null)
            {
                ErrorMessage = $"Error from external provider: {remoteError}";
                return RedirectToAction(nameof(Login));
            }
            var info = await _signInManager.GetExternalLoginInfoAsync();
            if (info == null)
            {
                return RedirectToAction(nameof(Login));
            }

            // Sign in the user with this external login provider if the user already has a login.
            var result = await _signInManager.ExternalLoginSignInAsync(info.LoginProvider, info.ProviderKey, isPersistent: false, bypassTwoFactor: true);
            if (result.Succeeded)
            {
                _logger.LogInformation("User logged in with {Name} provider.", info.LoginProvider);
                return RedirectToLocal(returnUrl);
            }
            if (result.IsLockedOut)
            {
                return RedirectToAction(nameof(Lockout));
            }
            else
            {
                // If the user does not have an account, then ask the user to create an account.
                ViewData["ReturnUrl"] = returnUrl;
                ViewData["LoginProvider"] = info.LoginProvider;
                var email = info.Principal.FindFirstValue(ClaimTypes.Email);
                return View("ExternalLogin", new ExternalLoginViewModel { Email = email });
            }
        }

        [HttpPost]
        [AllowAnonymous]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ExternalLoginConfirmation(ExternalLoginViewModel model, string returnUrl = null)
        {
            if (ModelState.IsValid)
            {
                // Get the information about the user from the external login provider
                var info = await _signInManager.GetExternalLoginInfoAsync();
                if (info == null)
                {
                    throw new ApplicationException("Error loading external login information during confirmation.");
                }
                var user = new ApplicationUser { UserName = model.Email, Email = model.Email };
                var result = await _userManager.CreateAsync(user);
                if (result.Succeeded)
                {
                    result = await _userManager.AddLoginAsync(user, info);
                    if (result.Succeeded)
                    {
                        await _signInManager.SignInAsync(user, isPersistent: false);
                        _logger.LogInformation("User created an account using {Name} provider.", info.LoginProvider);
                        return RedirectToLocal(returnUrl);
                    }
                }
                AddErrors(result);
            }

            ViewData["ReturnUrl"] = returnUrl;
            return View(nameof(ExternalLogin), model);
        }

        [HttpGet]
        [AllowAnonymous]
        public async Task<IActionResult> ConfirmEmail(string userId, string code)
        {
            if (userId == null || code == null)
            {
                return RedirectToAction(nameof(HomeController.Dashboard), "Home");
            }
            var user = await _userManager.FindByIdAsync(userId);
            if (user == null)
            {
                throw new ApplicationException($"Unable to load user with ID '{userId}'.");
            }
            var result = await _userManager.ConfirmEmailAsync(user, code);
            return View(result.Succeeded ? "ConfirmEmail" : "Error");
        }

        [HttpGet]
        [AllowAnonymous]
        public IActionResult ForgotPassword()
        {
            //var model = new ForgotPasswordViewModel {  };
            return View();
        }

        [HttpPost]
        [AllowAnonymous]
        //[ValidateAntiForgeryToken]
        public async Task<IActionResult> ForgotPassword(ForgotPasswordViewModel model)
        {
            if (ModelState.IsValid)
            {
                var user = await _userManager.FindByEmailAsync(model.Email);

                var chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789";
                var stringChars = new char[6];
                var random = new Random();

                for (int i = 0; i < stringChars.Length; i++)
                {
                    stringChars[i] = chars[random.Next(chars.Length)];
                }

                var token = new String(stringChars);

                MailMessage m = new MailMessage(new MailAddress("no-reply@emmares.com"), new MailAddress(model.Email));
                m.Subject = "EMMARES MVP Password Reset";
                m.Body = "<html><head><meta http - equiv = \"Content-Type\" content = \"text/html; charset=utf-8\" ><title> EMMARES.io </title>" +
                    "<meta name=\"viewport\" content=\"width = device - width, initial - scale = 1.0, minimum - scale = 1.0, maximum - scale = 1.0, user - scalable = no\">" +
                    "<link href=\"http://fonts.googleapis.com/css?family=Muli:300|Open+Sans:300,400,700\" rel=\"stylesheet\" type=\"text/css\">" +
                    "<style type=\"text / css\"> .ExternalClass, .ExternalClass p, .ExternalClass span, .ExternalClass font, .ExternalClass td, .ExternalClass div { line-height: 100%; }" +
                    "body { -webkit-text-size-adjust: none; -ms-text-size-adjust: none; } body, img, div, p, ul, li, span, strong, a { margin: 0; padding: 0; }" +
                    "table { border-spacing: 0; } table td { border-collapse: collapse; }" +
                    "body, #body_style { width: 100% !important; color: #737373; font-family: 'Open Sans', sans-serif; font-weight: 400; font-size: 14px; line-height: 1.2; background: #ffffff; }" +
                    "a { color: #737373; text-decoration: none; outline: none; } a:link { color: #737373; text-decoration: none; } a:visited { color: #737373; text-decoration: none; }" +
                    "a:hover { text-decoration: none !important; } a[href^=\"tel\"], a[href^=\"sms\"] { text-decoration: none; color: #737373 !important; pointer-events: none; cursor: default; }" +
                    "img { border: none !important; outline: none !important; text-decoration: none; height: auto; max-width: 100%; display: block; }" +
                    "a { border: none !important; outline: none !important; } table { border-collapse: collapse; mso-table-lspace: 0pt; mso-table-rspace: 0pt; }" +
                    "tr, td { margin: 0; padding: 0; }" +
                    "@media screen and (max-width: 639px) { table[class=\"wrapper\"]{ width: 100% !important; } td[class=\"width - 10\"]{ width: 10px !important; }" +
                    "td[class=\"gutter - 20\"]{ width: 20px !important; } table[class=\"height - auto\"]{ height: auto !important; }" +
                    "td[class=\"height - 175\"]{ height: 175px !important; } td[class=\"font - 14\"]{ font-size: 14px !important; }" +
                    "td[class=\"height - 30\"]{ height: 30px !important; } td[class=\"font - 14 editable\"]{ font-size: 14px !important; } }" +
                    "@media screen and (max-width: 479px) { td[class=\"device - 480\"]{ width: 100% !important; display: block; } table[class=\"device - 480\"]{ width: 100% !important; }" +
                    "td[class=\"gutter - 20\"]{ width: 15px !important; } td[class=\"height - 150\"]{ height: 175px !important; } td[class=\"height - 35\"]{ height: 35px !important; }" +
                    "td[class=\"font - 25\"]{ font-size: 25px !important; } td img[class=\"img - width\"]{ width: 100% !important; height: auto !important; }" +
                    "table[class=\"border - btm\"]{ border-top: 1px dashed #979797; } table[class=\"border - top\"]{ border-top: 1px dashed #ffffff; }" +
                    "td[class=\"font - 25 editable\"]{ font-size: 25px !important; } }" +
                    "@media screen and (max-width: 360px) { td[class=\"gutter - 20\"]{ width: 10px !important; } td[class=\"font - 25\"]{ font-size: 20px !important; } td[class=\"font - 25 editable\"]{" +
                    "font-size: 20px !important; } }" +
                    "@media screen and (max-width: 320px) { td[class=\"font - small\"]{ font-size: 10px !important; } td[class=\"font - small editable\"]{ font-size: 10px !important; } } </style>" +
                    "</head><body style =\"width:100% !important; color:#737373; font-family: 'Open Sans', sans-serif, Arial; font-size:14px; font-weight: 400; line-height:1.2;\" alink=\"#737373\" link=\"#737373\" text=\"#737373\">" +
                    "<table cellpadding=\"0\" cellspacing=\"0\" border=\"0\" width=\"640\" align=\"center\" class=\"wrapper\" style=\"margin: auto; background: #ffffff;\"><tbody><tr><td>" +
                    "<table width=\"100%\" border=\"0\" cellpadding=\"0\" cellspacing=\"0\"><tbody><tr><td class=\"gutter-20\" width=\"45\"><img border=\"0\" src=\"https://emmares.io/images/spacer.gif\" alt=\"\" width=\"1\" height=\"1\"></td><td>" +
                    "<table width=\"100%\" border=\"0\" cellpadding=\"0\" cellspacing=\"0\"><tbody><tr><td height=\"32\"><img border=\"0\" src=\"https://emmares.io/images/spacer.gif\" alt=\"\" width=\"1\" height=\"1\"></td></tr><tr>" +
                    "<td><table width=\"100%\" border=\"0\" cellpadding=\"0\" cellspacing=\"0\"><tbody><tr><td class=\"device-480\" width=\"7\"><img border=\"0\" src=\"https://emmares.io/images/spacer.gif\" alt=\"\" width=\"1\" height=\"1\"></td>" +
                    "<td class=\"device-480\" width=\"104\"><table width=\"100%\" border=\"0\" cellpadding=\"0\" cellspacing=\"0\"><tbody><tr><td>" +
                    "<a class=\"editable-lni\" href=\"https://emmares.io/\" style=\"display: block;\" data-selector=\"a.editable-lni\"><img border=\"0\" src=\"https://emmares.io/images/logo.png\" alt=\"\" width=\"104\" height=\"30\" style=\"display: block; margin: auto;\"></a>" +
                    "</td></tr></tbody></table></td><td class=\"device-480\" width=\"10\" height=\"25\"><img border=\"0\" src=\"https://emmares.io/images/spacer.gif\" alt=\"\" width=\"1\" height=\"1\"></td>" +
                    "<td class=\"device-480\"><table width=\"100%\" border=\"0\" cellpadding=\"0\" cellspacing=\"0\"><tbody><tr><td><table class=\"device-480\" border=\"0\" cellpadding=\"0\" cellspacing=\"0\" align=\"right\">" +
                    "<tbody><tr><td align=\"center\"></td></tr></tbody></table></td></tr></tbody></table></td></tr></tbody></table></td></tr><tr><td height=\"29\"><img border=\"0\" src=\"https://emmares.io/images/spacer.gif\" alt=\"\" width=\"1\" height=\"1\"></td></tr></tbody></table></td>" +
                    "<td class=\"gutter-20\" width=\"35\"><img border=\"0\" src=\"https://emmares.io/images/spacer.gif\" alt=\"\" width=\"1\" height=\"1\"></td></tr></tbody></table></td></tr>" +
                    "</tbody></table><table cellpadding=\"0\" cellspacing=\"0\" border=\"0\" width=\"640\" align=\"center\" class=\"wrapper\" style=\"margin: auto; background-color: #ffffff;\"><tbody><tr><td>" +
                    "<table width=\"100%\" border=\"0\" cellpadding=\"0\" cellspacing=\"0\"><tbody><tr>" +
                    "<td class=\"gutter-20\" width=\"45\"><img border=\"0\" src=\"https://emmares.io/images/spacer.gif\" alt=\"\" width=\"1\" height=\"1\"></td><td>" +
                    "<table width=\"100%\" border=\"0\" cellpadding=\"0\" cellspacing=\"0\"><tbody><tr>" +
                    "<td class=\"height-35\" height=\"\"><img border=\"0\" src=\"https://emmares.io/images/spacer.gif\" alt=\"\" width=\"1\" height=\"1\"></td></tr><tr><td>" +
                    "<table width=\"100%\" border=\"0\" cellpadding=\"0\" cellspacing=\"0\"><tbody><tr>" +
                    "<td height=\"15\"><img border=\"0\" src=\"https://emmares.io/images/spacer.gif\" alt=\"\" width=\"1\" height=\"1\"></td></tr><tr>" +
                    "<td class=\"editable\" style=\"padding: 0px; margin: 0px; font-family: 'Open Sans', sans-serif; font-weight: 700; color: #CD994B; font-size: 18px; line-height: 1.2; text-transform: uppercase; letter-spacing: 0.1em;\" data-selector=\"td.editable\">" +
                    "Forget password? Let's get you a new one.</td></tr><tr><td height=\"21\"><img border=\"0\" src=\"https://emmares.io/images/spacer.gif\" alt=\"\" width=\"1\" height=\"1\"></td></tr><tr>" +
                    "<td class=\"editable\" style=\"padding: 0px; margin: 0px; font-family: 'Open Sans', sans-serif; font-weight: 400; color: #000000; font-size: 14px; line-height: 1.7; letter-spacing: 0.1em;\" data-selector=\"td.editable\">" +
                    "You are receiving this email because we received a passord reset request for your account.</br></br><a href=\"http://emmares.com/Account/ResetPassword?sp=tok&t=" + token + "\" style=\"text-decoration:underline; color:#000000;\"><strong>RESET YOUR PASSWORD!</strong></a>" +
                    "</br></br><span style=\"color:#737373;\">If you're having trouble clicking the \"RESET YOUR PASSWORD!\" button, copy and paste the URL below into your web browser:</span></br>" +
                    "<a href=\"http://emmares.com/Account/ResetPassword?sp=tok&t=" + token + "\" style=\"text-decoration:underline; color:#737373;\">http://emmares.com/Account/ResetPassword?sp=tok&t=" + token + "</a></br></br>" +
                    "If you did not request a password reset, no further action is required.</td></tr><tr><td height=\"15\"><img border=\"0\" src=\"https://emmares.io/images/spacer.gif\" alt=\"\" width=\"1\" height=\"1\"></td>" +
                    "</tr></tbody></table></td></tr><tr><td class=\"height-35\" height=\"48\"><img border=\"0\" src=\"https://emmares.io/images/spacer.gif\" alt=\"\" width=\"1\" height=\"1\"></td>" +
                    "</tr></tbody></table></td><td class=\"gutter-20\" width=\"45\"><img border=\"0\" src=\"https://emmares.io/images/spacer.gif\" alt=\"\" width=\"1\" height=\"1\"></td></tr>" +
                    "</tbody></table></td></tr></tbody></table><table cellpadding=\"0\" cellspacing=\"0\" border=\"0\" width=\"640\" align=\"center\" class=\"wrapper\" style=\"margin: auto; background-color: #f9f9f9;\">" +
                    "<tbody><tr><td><table width=\"100%\" border=\"0\" cellpadding=\"0\" cellspacing=\"0\"><tbody><tr><td><table width=\"100%\" border=\"0\" cellpadding=\"0\" cellspacing=\"0\">" +
                    "<tbody><tr><td height=\"\"><img border=\"0\" src=\"https://emmares.io/images/spacer.gif\" alt=\"\" width=\"1\" height=\"1\"></td></tr><tr><td>" +
                    "<table width=\"100%\" border=\"0\" cellpadding=\"0\" cellspacing=\"0\"><tbody><tr><td valign=\"top\" class=\"device-480\" width=\"100%\">" +
                    "<table width=\"100%\" border=\"0\" cellpadding=\"0\" cellspacing=\"0\"><tbody><tr>" +
                    "<td class=\"gutter-20\" width=\"40\"><img border=\"0\" src=\"https://emmares.io/images/spacer.gif\" alt=\"\" width=\"1\" height=\"1\"></td><td>" +
                    "<table width=\"100%\" border=\"0\" cellpadding=\"0\" cellspacing=\"0\"><tbody><tr>" +
                    "<td class=\"editable\" style=\"text-align: center; padding: 0px; margin: 0px; font-family: 'Open Sans', sans-serif; font-weight: 400; color: #515151; font-size: 12px; line-height: 1.2; letter-spacing: 0.1em;\" data-selector=\"td.editable\">" +
                    "Copyright © EMMARES.io  </td></tr></tbody></table></td><td class=\"gutter-20\" width=\"40\"><img border=\"0\" src=\"https://emmares.io/images/spacer.gif\" alt=\"\" width=\"1\" height=\"1\"></td>" +
                    "</tr></tbody></table></td></tr></tbody></table></td></tr><tr><td height=\"37\"><img border=\"0\" src=\"https://emmares.io/images/spacer.gif\" alt=\"\" width=\"1\" height=\"1\"></td></tr>" +
                    "</tbody></table></td></tr></tbody></table></td></tr></tbody></table></body></html>";
                m.IsBodyHtml = true;
                SmtpClient smtp = new SmtpClient("localhost");
                smtp.Send(m);
                return View("ForgotPasswordConfirmation");
            }

            // If we got this far, something failed, redisplay form
            return View(model);
        }

        [HttpGet]
        [AllowAnonymous]
        public IActionResult ForgotPasswordConfirmation()
        {
            return View();
        }

        [HttpGet]
        [AllowAnonymous]
        public IActionResult ResetPassword(string code = null)
        {
            //if (code == null)
            //{
            //    throw new ApplicationException("A code must be supplied for password reset.");
            //}
            var model = new ResetPasswordViewModel { Code = code };
            return View(model);
        }

        [HttpPost]
        [AllowAnonymous]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ResetPassword(ResetPasswordViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }
            var user = await _userManager.FindByEmailAsync(model.Email);
            if (user == null)
            {
                // Don't reveal that the user does not exist
                return RedirectToAction(nameof(ResetPasswordConfirmation));
            }
            var code = await _userManager.GeneratePasswordResetTokenAsync(user);
            model.Code = code;

            var callbackUrl = Url.Action("ResetPassword", "Account", new { userId = user.Id, code = code }, protocol: HttpContext.Request.Scheme);
            //await _emailSender.SendEmailAsync(user.Email, "Reset Password", $"Please reset your password by clicking here: <a href='{callbackUrl}'>link</a>");
            //return View("ForgotPasswordConfirmation");

            var result = await _userManager.ResetPasswordAsync(user, model.Code, model.Password);
            if (result.Succeeded)
            {
                return RedirectToAction(nameof(ResetPasswordConfirmation));
            }
            AddErrors(result);
            return View();
        }

        [HttpGet]
        [AllowAnonymous]
        public IActionResult ResetPasswordConfirmation()
        {
            return View();
        }

        [HttpGet]
        public IActionResult AccessDenied()
        {
            return View();
        }

        private void SetBaseUrl()
        {
            if (string.IsNullOrWhiteSpace(Paths.BaseUrl)) { Paths.BaseUrl = Request.Scheme + "://" + Request.Host + Request.PathBase + "/"; }
        }

        private static object budgetLock = new object();

        // GET: Account/Stars
        [HttpGet]
        [AllowAnonymous]
        public async Task<IActionResult> Stars(int? stars, string cid)
        {
            SetBaseUrl();

            var user = await _userManager.GetUserAsync(User);

            if (user == null)
            {
                return Redirect(Paths.BaseUrl + "Account/Login?ReturnUrl=" + Paths.BasePath.Replace("/", "%2F") + "Account%2FStars?stars=" + stars + "%26cid=" + cid);
            }
            else
            {
                Campaign c;

                lock (budgetLock)
                {
                    if (!_cache.TryGetValue(cid, out c))
                    {
                        c = _db.Campaigns.FirstOrDefault(x => x.ID.ToString().ToLower() == cid.ToLower());
                        var cacheEntryOptions = new MemoryCacheEntryOptions().SetSlidingExpiration(TimeSpan.FromMinutes(10));
                        _cache.Set(cid, c, cacheEntryOptions);
                    }
                    else
                    {
                        _db.Entry(c).State = Microsoft.EntityFrameworkCore.EntityState.Unchanged;
                    }
                }

                if (c == null)
                {
                    return NotFound();
                }

                ViewBag.Tokens = 0;
                lock (c) // TODO allow budgeting tolerance, etc. to make it lock free?
                {
                    if (_db.Statistics.Where(x => x.User == user && x.Campaign == c).Count() == 0) // do not allow same user to rate twice
                    {
                        var reward = 0;
                        if (c.Budget > 0)
                        {
                            reward = 1;
                            c.Budget -= reward;
                            var u = _db.Users.First(x => x.Id == user.Id);
                            u.Balance += reward;
                        }

                        var s = new Statistic { Campaign = c, DateAdded = DateTime.Now, DateModified = DateTime.Now, Rating = stars ?? 0, Reward = reward, User = user };
                        _db.Add(s);
                        _db.SaveChanges();

                        var doc = new StatisticsDocES { campaign_id = c.ID.ToString().ToLower (), date_added = DateTime.Now, rating = stars ?? 0, reward = reward, user_Id = user.Id.ToLower () };
                        StatisticsES.UpdateDocument(doc);

                        ViewBag.Tokens = reward;
                    }
                }
                return View();
            }
        }

        #region Helpers

        private void AddErrors(IdentityResult result)
        {
            foreach (var error in result.Errors)
            {
                ModelState.AddModelError(string.Empty, error.Description);
            }
        }

        private IActionResult RedirectToLocal(string returnUrl)
        {
            if (Url.IsLocalUrl(returnUrl))
            {
                return Redirect(returnUrl);
            }
            else
            {
                return RedirectToAction(nameof(HomeController.Dashboard), "Home");
            }
        }

        #endregion
    }
}
