// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
#nullable enable

using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;
using CampusConnect.Data;
using CampusConnect.Models.MongoDB;
using CampusConnect.Services;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using MongoDB.Bson;

namespace CampusConnect.Areas.Identity.Pages.Account
{
    [AllowAnonymous]
    public class LoginModel : PageModel
    {
        private readonly SignInManager<IdentityUser> _signInManager;
        private readonly ILogger<LoginModel> _logger;
        private readonly MongoDBService _mongoService;
        private readonly UserManager<IdentityUser> _userManager;
        private readonly TablesDbContext _tablesDb;

        private string? ClientIp => HttpContext?.Connection?.RemoteIpAddress?.ToString();
        private string TraceId => HttpContext?.TraceIdentifier ?? "";

        public LoginModel(
            SignInManager<IdentityUser> signInManager, 
            ILogger<LoginModel> logger,
            MongoDBService mongoService,
            UserManager<IdentityUser> userManager,
            TablesDbContext tablesDb)
        {
            _signInManager = signInManager;
            _logger = logger;
            _mongoService = mongoService;
            _userManager = userManager;
            _tablesDb = tablesDb;
        }

        [BindProperty]
        public InputModel Input { get; set; } = new();

        public IList<AuthenticationScheme> ExternalLogins { get; set; } = new List<AuthenticationScheme>();

        public string ReturnUrl { get; set; } = "~/";

        [TempData]
        public string ErrorMessage { get; set; } = "";

        public class InputModel
        {
            [Required]
            [EmailAddress]
            public string Email { get; set; } = "";

            [Required]
            [DataType(DataType.Password)]
            public string Password { get; set; } = "";

            [Display(Name = "Remember me?")]
            public bool RememberMe { get; set; }
        }

        public async Task OnGetAsync(string? returnUrl = null)
        {
            if (!string.IsNullOrEmpty(ErrorMessage))
            {
                ModelState.AddModelError(string.Empty, ErrorMessage);
            }

            returnUrl ??= Url.Content("~/");

            // Clear the existing external cookie to ensure a clean login process
            await HttpContext.SignOutAsync(IdentityConstants.ExternalScheme);

            ExternalLogins = (await _signInManager.GetExternalAuthenticationSchemesAsync()).ToList();
            ReturnUrl = returnUrl;
        }

        public async Task<IActionResult> OnPostAsync(string? returnUrl = null)
        {
            returnUrl ??= Url.Content("~/");

            ExternalLogins = (await _signInManager.GetExternalAuthenticationSchemesAsync()).ToList();
            ReturnUrl = returnUrl;

            if (!ModelState.IsValid)
            {
                _logger.LogWarning("LOGIN POST INVALID MODELSTATE. Email={Email} IP={IP} TraceId={TraceId}",
                    Input?.Email, ClientIp, TraceId);
                return Page();
            }

            // lockoutOnFailure: true ensures repeated failures can lock the account (if configured)
            var result = await _signInManager.PasswordSignInAsync(
                Input.Email,
                Input.Password,
                Input.RememberMe,
                lockoutOnFailure: true
            );

            if (result.Succeeded)
            {
                _logger.LogInformation("LOGIN SUCCESS. Email={Email} IP={IP} TraceId={TraceId}",
                    Input.Email, ClientIp, TraceId);

                var user = await _userManager.FindByEmailAsync(Input.Email);
                if (user != null)
                {
                    // Check if user needs to change password
                    var appUser = await _tablesDb.users
                        .FirstOrDefaultAsync(u => u.identityUserId == user.Id);

                    if (appUser != null && appUser.RequirePasswordChange)
                    {
                        _logger.LogInformation("User {Email} requires password change. Redirecting to FirstLoginPasswordChange.", Input.Email);
                        return RedirectToPage("./FirstLoginPasswordChange");
                    }

                    // Log successful login to MongoDB
                    var roles = await _userManager.GetRolesAsync(user);
                    await _mongoService.LogActivityAsync(new ActivityLog
                    {
                        UserId = user.Id,
                        UserName = user.Email ?? "Unknown",
                        UserRole = roles.FirstOrDefault() ?? "User",
                        Action = "login_success",
                        RequestId = null, // No request involved
                        Details = new BsonDocument
                        {
                            { "rememberMe", Input.RememberMe },
                            { "userAgent", Request.Headers["User-Agent"].ToString() }
                        },
                        IpAddress = ClientIp
                    });
                }

                return LocalRedirect(returnUrl);
            }

            if (result.RequiresTwoFactor)
            {
                _logger.LogInformation("LOGIN REQUIRES 2FA. Email={Email} IP={IP} TraceId={TraceId}",
                    Input.Email, ClientIp, TraceId);
                return RedirectToPage("./LoginWith2fa", new { ReturnUrl = returnUrl, RememberMe = Input.RememberMe });
            }

            if (result.IsLockedOut)
            {
                _logger.LogWarning("LOGIN LOCKED OUT. Email={Email} IP={IP} TraceId={TraceId}",
                    Input.Email, ClientIp, TraceId);

                // Log lockout event
                var user = await _userManager.FindByEmailAsync(Input.Email);
                if (user != null)
                {
                    await _mongoService.LogActivityAsync(new ActivityLog
                    {
                        UserId = user.Id,
                        UserName = Input.Email,
                        UserRole = "Unknown",
                        Action = "login_locked_out",
                        RequestId = null,
                        Details = new BsonDocument
                        {
                            { "reason", "Account locked due to multiple failed attempts" }
                        },
                        IpAddress = ClientIp
                    });
                }

                return RedirectToPage("./Lockout");
            }

            // Generic failed login (wrong password, unknown user, etc.)
            _logger.LogWarning("LOGIN FAILED. Email={Email} IP={IP} TraceId={TraceId}",
                Input.Email, ClientIp, TraceId);

            // Log failed attempt (even for non-existent users)
            await _mongoService.LogActivityAsync(new ActivityLog
            {
                UserId = "anonymous",
                UserName = Input.Email,
                UserRole = "Unknown",
                Action = "login_failed",
                RequestId = null,
                Details = new BsonDocument
                {
                    { "reason", "Invalid credentials" }
                },
                IpAddress = ClientIp
            });

            ModelState.AddModelError(string.Empty, "Invalid login attempt.");
            return Page();
        }
    }
}