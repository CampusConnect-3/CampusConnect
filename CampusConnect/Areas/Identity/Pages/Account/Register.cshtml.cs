// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
#nullable enable

using System;
using System.ComponentModel.DataAnnotations;
using System.Threading.Tasks;
using CampusConnect.Constants;
using CampusConnect.Data;
using CampusConnect.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.Logging;

namespace CampusConnect.Areas.Identity.Pages.Account
{
    [AllowAnonymous]
    public class RegisterModel : PageModel
    {
        private readonly SignInManager<IdentityUser> _signInManager;
        private readonly UserManager<IdentityUser> _userManager;
        private readonly ILogger<RegisterModel> _logger;
        private readonly TablesDbContext _tablesDb;

        public RegisterModel(
            UserManager<IdentityUser> userManager,
            SignInManager<IdentityUser> signInManager,
            ILogger<RegisterModel> logger,
            TablesDbContext tablesDb)
        {
            _userManager = userManager;
            _signInManager = signInManager;
            _logger = logger;
            _tablesDb = tablesDb;
        }

        [BindProperty]
        public InputModel Input { get; set; } = default!;

        public string ReturnUrl { get; set; } = "/";

        public class InputModel
        {
            [Required, Display(Name = "First name")]
            public string FirstName { get; set; } = string.Empty;

            [Required, Display(Name = "Last name")]
            public string LastName { get; set; } = string.Empty;

            [Required, EmailAddress]
            public string Email { get; set; } = string.Empty;

            [Required, DataType(DataType.Password)]
            public string Password { get; set; } = string.Empty;

            [Required, DataType(DataType.Password), Compare(nameof(Password), ErrorMessage = "Passwords do not match.")]
            public string ConfirmPassword { get; set; } = string.Empty;

            [Display(Name = "Department")]
            public string? Department { get; set; }
        }

        public void OnGet(string? returnUrl = null)
        {
            ReturnUrl = string.IsNullOrWhiteSpace(returnUrl) ? Url.Content("~/") : returnUrl;
        }

        public async Task<IActionResult> OnPostAsync(string? returnUrl = null)
        {
            ReturnUrl = string.IsNullOrWhiteSpace(returnUrl) ? Url.Content("~/") : returnUrl;

            if (!ModelState.IsValid)
                return Page();

            var identityUser = new IdentityUser
            {
                UserName = Input.Email,
                Email = Input.Email
            };

            // 1) Create Identity user
            var createResult = await _userManager.CreateAsync(identityUser, Input.Password);
            if (!createResult.Succeeded)
            {
                foreach (var err in createResult.Errors)
                    ModelState.AddModelError(string.Empty, err.Description);

                return Page();
            }

            _logger.LogInformation("Identity user created. IdentityUserId={IdentityUserId}", identityUser.Id);

            // 2) Assign Pending role
            var roleResult = await _userManager.AddToRoleAsync(identityUser, Roles.Pending.ToString());
            if (!roleResult.Succeeded)
            {
                // If role assignment fails, remove Identity user to avoid half-created accounts
                await _userManager.DeleteAsync(identityUser);
                ModelState.AddModelError(string.Empty, "Unable to assign role. Please contact an administrator.");
                return Page();
            }

            // 3) Create app user row (linked to AspNetUsers)
            var appUser = new user
            {
                fName = Input.FirstName,
                lName = Input.LastName,
                username = identityUser.UserName ?? Input.Email,
                email = identityUser.Email ?? Input.Email,
                department = Input.Department,
                status = Roles.Pending.ToString(), // <-- CHANGED: Use constant instead of hardcoded "Pending"
                identityUserId = identityUser.Id
            };

            try
            {
                _tablesDb.users.Add(appUser);
                await _tablesDb.SaveChangesAsync();
            }
            catch (Exception ex)
            {
                // Roll back Identity user if app row fails (prevents orphan Identity accounts)
                _logger.LogError(ex, "Failed to create app user row. Rolling back Identity user. IdentityUserId={IdentityUserId}", identityUser.Id);
                await _userManager.DeleteAsync(identityUser);

                ModelState.AddModelError(string.Empty, "Unable to create profile. Please try again or contact an administrator.");
                return Page();
            }

            // 4) Sign in and send them to PendingApproval (clean experience)
            await _signInManager.SignInAsync(identityUser, isPersistent: false);
            return RedirectToPage("/PendingApproval");
        }
    }
}