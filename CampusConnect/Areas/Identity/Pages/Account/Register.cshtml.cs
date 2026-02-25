// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
#nullable enable

using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Text.Encodings.Web;
using System.Threading;
using System.Threading.Tasks;
using RoleConstants = CampusConnect.Constants.Roles;
using CampusConnect.Data;
using CampusConnect.Models;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.UI.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace CampusConnect.Areas.Identity.Pages.Account
{
    [AllowAnonymous]
    public class RegisterModel : PageModel
    {
        private readonly SignInManager<ApplicationUser> _signInManager;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly IUserStore<ApplicationUser> _userStore;
        private readonly IUserEmailStore<ApplicationUser> _emailStore;
        private readonly ILogger<RegisterModel> _logger;
        private readonly IEmailSender _emailSender;
        private readonly TablesDbContext _tablesDb;
        private readonly IWebHostEnvironment _env;

        public RegisterModel(
            UserManager<ApplicationUser> userManager,
            IUserStore<ApplicationUser> userStore,
            SignInManager<ApplicationUser> signInManager,
            ILogger<RegisterModel> logger,
            IEmailSender emailSender,
            TablesDbContext tablesDb,
            IWebHostEnvironment env)
        {
            _userManager = userManager;
            _userStore = userStore;
            _emailStore = GetEmailStore();
            _signInManager = signInManager;
            _logger = logger;
            _emailSender = emailSender;
            _tablesDb = tablesDb;
            _env = env;
        }

        [BindProperty]
        public InputModel Input { get; set; } = default!;

        public string ReturnUrl { get; set; } = default!;
        public IList<AuthenticationScheme> ExternalLogins { get; set; } = new List<AuthenticationScheme>();

        public class InputModel
        {
            [Required(ErrorMessage = "First name is required.")]
            [StringLength(50)]
            [Display(Name = "First name")]
            public string FirstName { get; set; } = string.Empty;

            [Required(ErrorMessage = "Last name is required.")]
            [StringLength(50)]
            [Display(Name = "Last name")]
            public string LastName { get; set; } = string.Empty;

            [Required(ErrorMessage = "ID number is required.")]
            [StringLength(20)]
            [Display(Name = "ID number")]
            public string SchoolId { get; set; } = string.Empty;

            [Required(ErrorMessage = "Role is required.")]
            public string Role { get; set; } = string.Empty; // Student | Staff | Admin

            [Display(Name = "Class year")]
            public string? ClassYear { get; set; } // Student only

            [Display(Name = "Department")]
            public string? Department { get; set; } // Staff only

            [Phone]
            [StringLength(25)]
            [Display(Name = "Phone number")]
            public string? PhoneNumber { get; set; }

            [Required]
            [EmailAddress]
            public string Email { get; set; } = string.Empty;

            [Required]
            [StringLength(100, ErrorMessage = "The {0} must be at least {2} and at max {1} characters long.", MinimumLength = 6)]
            [DataType(DataType.Password)]
            public string Password { get; set; } = string.Empty;

            [DataType(DataType.Password)]
            [Compare(nameof(Password), ErrorMessage = "The password and confirmation password do not match.")]
            public string ConfirmPassword { get; set; } = string.Empty;
        }

        public async Task OnGetAsync(string? returnUrl = null)
        {
            ReturnUrl = returnUrl ?? Url.Content("~/");
            ExternalLogins = (await _signInManager.GetExternalAuthenticationSchemesAsync()).ToList();
        }

        public async Task<IActionResult> OnPostAsync(string? returnUrl = null)
        {
            ReturnUrl = returnUrl ?? Url.Content("~/");
            ExternalLogins = (await _signInManager.GetExternalAuthenticationSchemesAsync()).ToList();

            ValidateRoleSpecificFields();

            var schoolId = (Input.SchoolId ?? "").Trim();
            var email = (Input.Email ?? "").Trim();
            var normalizedRole = NormalizeRole(Input.Role);

            // Prevent duplicate SchoolId in legacy table
            if (!string.IsNullOrWhiteSpace(schoolId))
            {
                var existsSchoolId = _tablesDb.users.Any(u => u.username == schoolId);
                if (existsSchoolId)
                    ModelState.AddModelError("Input.SchoolId", "An account with this ID number already exists.");
            }

            // Prevent duplicate email in Identity
            if (!string.IsNullOrWhiteSpace(email))
            {
                var existingIdentity = await _userManager.FindByEmailAsync(email);
                if (existingIdentity != null)
                    ModelState.AddModelError("Input.Email", "An account with this email already exists.");
            }

            if (!ModelState.IsValid)
                return Page();

            ApplicationUser? identityUser = null;

            try
            {
                // 1) Create Identity user
                identityUser = CreateUser();

                await _userStore.SetUserNameAsync(identityUser, email, CancellationToken.None);
                await _emailStore.SetEmailAsync(identityUser, email, CancellationToken.None);

                identityUser.FirstName = Input.FirstName.Trim();
                identityUser.LastName = Input.LastName.Trim();
                identityUser.SchoolId = schoolId;
                identityUser.Role = normalizedRole;

                identityUser.ClassYear = normalizedRole == "Student" ? Input.ClassYear?.Trim() : null;
                identityUser.Department = normalizedRole == "Staff" ? Input.Department?.Trim() : null;

                if (!string.IsNullOrWhiteSpace(Input.PhoneNumber))
                    identityUser.PhoneNumber = Input.PhoneNumber.Trim();

                var createIdentity = await _userManager.CreateAsync(identityUser, Input.Password);
                if (!createIdentity.Succeeded)
                {
                    foreach (var error in createIdentity.Errors)
                        ModelState.AddModelError(string.Empty, error.Description);

                    return Page();
                }

                // Dev convenience: skip confirmation so login works immediately in Development
                if (_env.IsDevelopment())
                {
                    identityUser.EmailConfirmed = true;
                    await _userManager.UpdateAsync(identityUser);
                }

                // 2) Assign Identity role
                var identityRole = normalizedRole switch
                {
                    "Admin" => RoleConstants.Admin.ToString(),
                    "Staff" => RoleConstants.Staff.ToString(),
                    "Student" => RoleConstants.User.ToString(),
                    _ => RoleConstants.User.ToString()
                };

                var roleResult = await _userManager.AddToRoleAsync(identityUser, identityRole);
                if (!roleResult.Succeeded)
                {
                    foreach (var e in roleResult.Errors)
                        ModelState.AddModelError(string.Empty, e.Description);

                    return Page();
                }

                // 3) Create legacy profile row linked to AspNetUsers.Id
                // IMPORTANT: DbContext currently expects CampusConnect.Models.user (lowercase)
                var profile = new User
                {
                    IdentityUserId = identityUser.Id,
                    fName = identityUser.FirstName,
                    lName = identityUser.LastName,
                    username = identityUser.SchoolId,
                    password = "IDENTITY_ONLY",
                    email = email,
                    department = normalizedRole == "Staff" ? identityUser.Department : null,
                    status = "Active"
                };

                _tablesDb.users.Add(profile);
                await _tablesDb.SaveChangesAsync();

                _logger.LogInformation("User created. Email={Email} Role={Role}", email, identityRole);

                // 4) Email confirmation (mainly for production)
                var userId = await _userManager.GetUserIdAsync(identityUser);
                var code = await _userManager.GenerateEmailConfirmationTokenAsync(identityUser);
                code = WebEncoders.Base64UrlEncode(Encoding.UTF8.GetBytes(code));

                var callbackUrl = Url.Page(
                    "/Account/ConfirmEmail",
                    pageHandler: null,
                    values: new { area = "Identity", userId, code, returnUrl = ReturnUrl },
                    protocol: Request.Scheme);

                try
                {
                    await _emailSender.SendEmailAsync(
                        email,
                        "Confirm your email",
                        $"Please confirm your account by <a href='{HtmlEncoder.Default.Encode(callbackUrl!)}'>clicking here</a>.");
                }
                catch (Exception emailEx)
                {
                    _logger.LogError(emailEx, "Email send failed for {Email}", email);
                    TempData["StatusMessage"] = "Account created, but we couldn't send the confirmation email.";
                    return RedirectToPage("./Login");
                }

                TempData["StatusMessage"] =
                    (_userManager.Options.SignIn.RequireConfirmedAccount && !_env.IsDevelopment())
                        ? "Account created. Please confirm your email before logging in."
                        : "Account created. Please log in.";

                return RedirectToPage("./Login");
            }
            catch (Exception ex)
            {
                // Cleanup Identity user if anything fails after it was created
                if (identityUser != null)
                    await _userManager.DeleteAsync(identityUser);

                _logger.LogError(ex, "Registration failed.");

                ModelState.AddModelError(string.Empty,
                    _env.IsDevelopment()
                        ? $"Registration failed (DEV): {ex.Message}"
                        : "Registration failed. Please try again.");

                return Page();
            }
        }

        private void ValidateRoleSpecificFields()
        {
            var role = NormalizeRole(Input.Role);

            if (string.IsNullOrWhiteSpace(role))
            {
                ModelState.AddModelError("Input.Role", "Please select a role.");
                return;
            }

            if (role == "Student")
            {
                if (string.IsNullOrWhiteSpace(Input.ClassYear))
                    ModelState.AddModelError("Input.ClassYear", "Class year is required for students.");

                Input.Department = null;
            }
            else if (role == "Staff")
            {
                if (string.IsNullOrWhiteSpace(Input.Department))
                    ModelState.AddModelError("Input.Department", "Department is required for staff.");

                Input.ClassYear = null;
            }
            else if (role == "Admin")
            {
                Input.ClassYear = null;
                Input.Department = null;
            }
            else
            {
                ModelState.AddModelError("Input.Role", "Invalid role selection.");
            }
        }

        private static string NormalizeRole(string role)
        {
            role = (role ?? string.Empty).Trim();
            if (role.Equals("student", StringComparison.OrdinalIgnoreCase)) return "Student";
            if (role.Equals("staff", StringComparison.OrdinalIgnoreCase)) return "Staff";
            if (role.Equals("admin", StringComparison.OrdinalIgnoreCase)) return "Admin";
            return string.Empty;
        }

        private ApplicationUser CreateUser()
        {
            try
            {
                return Activator.CreateInstance<ApplicationUser>();
            }
            catch
            {
                throw new InvalidOperationException($"Can't create an instance of '{nameof(ApplicationUser)}'.");
            }
        }

        private IUserEmailStore<ApplicationUser> GetEmailStore()
        {
            if (!_userManager.SupportsUserEmail)
                throw new NotSupportedException("The default UI requires a user store with email support.");

            return (IUserEmailStore<ApplicationUser>)_userStore;
        }
    }
}