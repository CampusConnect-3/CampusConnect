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
using CampusConnect.Constants;   // Roles.*
using CampusConnect.Data;
using CampusConnect.Models;
using Microsoft.AspNetCore.Authentication;
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
    public class RegisterModel : PageModel
    {
        private readonly SignInManager<ApplicationUser> _signInManager;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly IUserStore<ApplicationUser> _userStore;
        private readonly IUserEmailStore<ApplicationUser> _emailStore;
        private readonly ILogger<RegisterModel> _logger;
        private readonly IEmailSender _emailSender;
        private readonly TablesDbContext _tablesDb;

        public RegisterModel(
            UserManager<ApplicationUser> userManager,
            IUserStore<ApplicationUser> userStore,
            SignInManager<ApplicationUser> signInManager,
            ILogger<RegisterModel> logger,
            IEmailSender emailSender,
            TablesDbContext tablesDb)
        {
            _userManager = userManager;
            _userStore = userStore;
            _emailStore = GetEmailStore();
            _signInManager = signInManager;
            _logger = logger;
            _emailSender = emailSender;
            _tablesDb = tablesDb;
        }

        [BindProperty]
        public InputModel Input { get; set; }

        public string ReturnUrl { get; set; }
        public IList<AuthenticationScheme> ExternalLogins { get; set; }

        public class InputModel
        {
            [Required(ErrorMessage = "First name is required.")]
            [StringLength(50)]
            public string FirstName { get; set; }

            [Required(ErrorMessage = "Last name is required.")]
            [StringLength(50)]
            public string LastName { get; set; }

            [Required(ErrorMessage = "ID number is required.")]
            [StringLength(20)]
            public string SchoolId { get; set; }

            [Required(ErrorMessage = "Role is required.")]
            public string Role { get; set; } // Student | Staff | Admin

            public string ClassYear { get; set; }   // Student only
            public string Department { get; set; }  // Staff only

            [Phone]
            [StringLength(25)]
            public string PhoneNumber { get; set; }

            [Required]
            [EmailAddress]
            public string Email { get; set; }

            [Required]
            [StringLength(100, ErrorMessage = "The {0} must be at least {2} and at max {1} characters long.", MinimumLength = 6)]
            [DataType(DataType.Password)]
            public string Password { get; set; }

            [DataType(DataType.Password)]
            [Compare("Password", ErrorMessage = "The password and confirmation password do not match.")]
            public string ConfirmPassword { get; set; }
        }

        public async Task OnGetAsync(string returnUrl = null)
        {
            ReturnUrl = returnUrl;
            ExternalLogins = (await _signInManager.GetExternalAuthenticationSchemesAsync()).ToList();
        }

        public async Task<IActionResult> OnPostAsync(string returnUrl = null)
        {
            returnUrl ??= Url.Content("~/");
            ExternalLogins = (await _signInManager.GetExternalAuthenticationSchemesAsync()).ToList();

            // Basic sanity check (prevents null ref if Input is ever null)
            if (Input == null)
            {
                ModelState.AddModelError(string.Empty, "Invalid registration submission.");
                return Page();
            }

            ValidateRoleSpecificFields();

            // Normalize inputs once
            var schoolId = (Input.SchoolId ?? "").Trim();
            var email = (Input.Email ?? "").Trim();
            var normalizedRole = NormalizeRole(Input.Role);

            // Prevent duplicate SchoolId registration (legacy profile table)
            if (!string.IsNullOrWhiteSpace(schoolId))
            {
                var existsSchoolId = _tablesDb.users.Any(u => u.username == schoolId);
                if (existsSchoolId)
                {
                    ModelState.AddModelError("Input.SchoolId", "An account with this ID number already exists.");
                }
            }

            // Prevent duplicate email registration (Identity)
            if (!string.IsNullOrWhiteSpace(email))
            {
                var existingIdentity = await _userManager.FindByEmailAsync(email);
                if (existingIdentity != null)
                {
                    ModelState.AddModelError("Input.Email", "An account with this email already exists.");
                }
            }

            if (!ModelState.IsValid)
                return Page();

            ApplicationUser identityUser = null;

            try
            {
                // 1) Create Identity user
                identityUser = CreateUser();

                await _userStore.SetUserNameAsync(identityUser, email, CancellationToken.None);
                await _emailStore.SetEmailAsync(identityUser, email, CancellationToken.None);

                identityUser.FirstName = Input.FirstName.Trim();
                identityUser.LastName = Input.LastName.Trim();
                identityUser.SchoolId = schoolId;

                // Store the UI role on the user profile (your custom field)
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

                // ✅ Best dev experience: auto-confirm email in Development
                // This prevents "Invalid login attempt" when RequireConfirmedAccount=true
                var env = HttpContext.RequestServices.GetService<IWebHostEnvironment>();
                if (env?.IsDevelopment() == true)
                {
                    identityUser.EmailConfirmed = true;
                    await _userManager.UpdateAsync(identityUser);
                }

                // Assign Identity role (Option A): Student -> User
                var identityRole = normalizedRole switch
                {
                    "Admin" => Roles.Admin.ToString(),
                    "Staff" => Roles.Staff.ToString(),
                    "Student" => Roles.User.ToString(), // Student becomes User in Identity roles
                    _ => Roles.User.ToString()
                };

                var roleResult = await _userManager.AddToRoleAsync(identityUser, identityRole);
                if (!roleResult.Succeeded)
                {
                    foreach (var e in roleResult.Errors)
                        ModelState.AddModelError(string.Empty, e.Description);

                    return Page();
                }

                // 2) Create linked legacy profile row
                // IMPORTANT: some legacy schemas require password/email NOT NULL.
                // We do NOT store real passwords here; Identity is the source of truth.
                var profile = new user
                {
                    IdentityUserId = identityUser.Id,
                    fName = identityUser.FirstName,
                    lName = identityUser.LastName,
                    username = identityUser.SchoolId,

                    // placeholders (do NOT store real passwords here)
                    password = "IDENTITY_ONLY",
                    email = email,

                    department = normalizedRole == "Staff" ? identityUser.Department : null,
                    status = "Active"
                };

                _tablesDb.users.Add(profile);
                await _tablesDb.SaveChangesAsync();

                _logger.LogInformation("User created: Identity + linked profile. Email={Email} Role={Role}", email, identityRole);

                // 3) Email confirmation (generate link)
                // If you auto-confirm in Development, this email is mostly for production use.
                var userId = await _userManager.GetUserIdAsync(identityUser);
                var code = await _userManager.GenerateEmailConfirmationTokenAsync(identityUser);
                code = WebEncoders.Base64UrlEncode(Encoding.UTF8.GetBytes(code));

                var callbackUrl = Url.Page(
                    "/Account/ConfirmEmail",
                    pageHandler: null,
                    values: new { area = "Identity", userId, code, returnUrl },
                    protocol: Request.Scheme);

                // Do NOT let email sending failure break registration (common in dev)
                try
                {
                    await _emailSender.SendEmailAsync(
                        email,
                        "Confirm your email",
                        $"Please confirm your account by <a href='{HtmlEncoder.Default.Encode(callbackUrl)}'>clicking here</a>.");
                }
                catch (Exception emailEx)
                {
                    _logger.LogError(emailEx, "Email send failed for {Email}", email);

                    TempData["StatusMessage"] =
                        "Account created, but we couldn't send the confirmation email. Please contact support.";

                    return RedirectToPage("./Login");
                }

                // After successful registration, redirect to Login (no auto sign-in).
                TempData["StatusMessage"] = (_userManager.Options.SignIn.RequireConfirmedAccount && (env?.IsDevelopment() != true))
                    ? "Account created. Please confirm your email before logging in."
                    : "Account created. Please log in.";

                return RedirectToPage("./Login");
            }
            catch (Exception ex)
            {
                // If profile creation fails after Identity user exists, clean up.
                if (identityUser != null)
                {
                    await _userManager.DeleteAsync(identityUser);
                }

                _logger.LogError(ex, "Registration failed.");

                var env = HttpContext.RequestServices.GetService<IWebHostEnvironment>();
                if (env?.IsDevelopment() == true)
                {
                    ModelState.AddModelError(string.Empty, $"Registration failed (DEV): {ex.Message}");
                }
                else
                {
                    ModelState.AddModelError(string.Empty, "Registration failed. Please try again.");
                }

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
            try { return Activator.CreateInstance<ApplicationUser>(); }
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