// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
#nullable disable

using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Text.Encodings.Web;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.UI.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.Extensions.Logging;
using CampusConnect.Data;
using CampusConnect.Models;

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

        public string ReturnUrl { get; set; } = default!;

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

            [DataType(DataType.Password), Compare(nameof(Password))]
            public string ConfirmPassword { get; set; } = string.Empty;

            [Display(Name = "Department")]
            public string? Department { get; set; }
        }

        public void OnGet(string? returnUrl = null)
        {
            ReturnUrl = returnUrl ?? Url.Content("~/");
        }

        public async Task<IActionResult> OnPostAsync(string? returnUrl = null)
        {
            returnUrl ??= Url.Content("~/");
            if (!ModelState.IsValid)
            {
                return Page();
            }

            var identityUser = new IdentityUser { UserName = Input.Email, Email = Input.Email };

            var createResult = await _userManager.CreateAsync(identityUser, Input.Password);
            if (!createResult.Succeeded)
            {
                foreach (var err in createResult.Errors)
                    ModelState.AddModelError(string.Empty, err.Description);
                return Page();
            }

            _logger.LogInformation("Identity user created.");

            // Now create the application user row and link it via email/username.
            var appUser = new user
            {
                fName = Input.FirstName,
                lName = Input.LastName,
                username = identityUser.UserName ?? Input.Email,
                email = identityUser.Email ?? Input.Email,
                department = Input?.Department,
                status = "Active" // or whatever default you want
            };

            try
            {
                _tablesDb.users.Add(appUser);
                await _tablesDb.SaveChangesAsync();
            }
            catch
            {
                // If app user creation fails, delete the Identity account to avoid orphans.
                await _userManager.DeleteAsync(identityUser);
                ModelState.AddModelError(string.Empty, "Unable to create profile. Please try again or contact an administrator.");
                return Page();
            }

            // Optionally sign in the user immediately
            await _signInManager.SignInAsync(identityUser, isPersistent: false);
            return LocalRedirect(returnUrl);
        }
    }
}
