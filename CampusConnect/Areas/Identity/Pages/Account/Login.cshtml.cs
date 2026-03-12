// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
#nullable enable

using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.Logging;

namespace CampusConnect.Areas.Identity.Pages.Account
{
    [AllowAnonymous]
    public class LoginModel : PageModel
    {
        private readonly SignInManager<IdentityUser> _signInManager;
        private readonly ILogger<LoginModel> _logger;

        private string? ClientIp => HttpContext?.Connection?.RemoteIpAddress?.ToString();
        private string TraceId => HttpContext?.TraceIdentifier ?? "";

        public LoginModel(SignInManager<IdentityUser> signInManager, ILogger<LoginModel> logger)
        {
            _signInManager = signInManager;
            _logger = logger;
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
                return RedirectToPage("./Lockout");
            }

            // Generic failed login (wrong password, unknown user, etc.)
            _logger.LogWarning("LOGIN FAILED. Email={Email} IP={IP} TraceId={TraceId}",
                Input.Email, ClientIp, TraceId);

            ModelState.AddModelError(string.Empty, "Invalid login attempt.");
            return Page();
        }
    }
}