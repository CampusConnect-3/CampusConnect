using CampusConnect.Data;
using CampusConnect.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace CampusConnect.Pages.Admin.UserPages
{
    [Authorize(Roles = "Admin")]
    public class CreateModel : PageModel
    {
        private readonly ApplicationDbContext _identityDb;
        private readonly TablesDbContext _tablesDb;
        private readonly UserManager<IdentityUser> _userManager;

        public CreateModel(ApplicationDbContext identityDb, TablesDbContext tablesDb, UserManager<IdentityUser> userManager)
        {
            _identityDb = identityDb;
            _tablesDb = tablesDb;
            _userManager = userManager;
        }

        [BindProperty]
        public InputModel Input { get; set; } = new();

        public List<SelectListItem> RoleOptions { get; set; } = new();

        public class InputModel
        {
            [Required, EmailAddress]
            public string Email { get; set; } = "";

            [Required, MinLength(6)]
            public string Password { get; set; } = "";

            [Required, Compare(nameof(Password))]
            public string ConfirmPassword { get; set; } = "";

            [Required]
            public string Role { get; set; } = "User";

            // Optional app-profile fields
            public string? FirstName { get; set; }
            public string? LastName { get; set; }
            public string? Department { get; set; }
        }

        public void OnGet()
        {
            LoadRoles();
        }

        public async Task<IActionResult> OnPostAsync(CancellationToken cancellationToken = default)
        {
            LoadRoles();

            if (!ModelState.IsValid)
                return Page();

            // Prevent duplicates in Identity
            var existingIdentity = await _userManager.FindByEmailAsync(Input.Email);
            if (existingIdentity != null)
            {
                ModelState.AddModelError(string.Empty, "That email already exists.");
                return Page();
            }

            // Check if username (email) already exists in app users table
            var existingAppUser = await _tablesDb.users
                .AnyAsync(u => u.username == Input.Email || u.email == Input.Email, cancellationToken);

            if (existingAppUser)
            {
                ModelState.AddModelError(string.Empty, "A user with that email/username already exists in the system.");
                return Page();
            }

            var user = new IdentityUser
            {
                UserName = Input.Email,
                Email = Input.Email,
                EmailConfirmed = true // lets them login even if RequireConfirmedAccount=true
            };

            var createResult = await _userManager.CreateAsync(user, Input.Password);
            if (!createResult.Succeeded)
            {
                foreach (var e in createResult.Errors)
                    ModelState.AddModelError(string.Empty, e.Description);

                return Page();
            }

            // Add role (must already exist from your DbSeeder)
            await _userManager.AddToRoleAsync(user, Input.Role);

            // Create app profile row
            var appUser = new user
            {
                email = Input.Email,
                username = Input.Email, // <-- FIX: Set username (required, unique index)
                fName = Input.FirstName ?? "N/A",
                lName = Input.LastName ?? "N/A",
                department = Input.Department,
                status = Input.Role == "Pending" ? "Pending" : "Active",
                identityUserId = user.Id
            };

            try
            {
                _tablesDb.users.Add(appUser);
                await _tablesDb.SaveChangesAsync(cancellationToken);
            }
            catch (DbUpdateException ex)
            {
                // Rollback Identity user if app profile fails
                await _userManager.DeleteAsync(user);
                ModelState.AddModelError(string.Empty, $"Failed to create user profile: {ex.InnerException?.Message ?? ex.Message}");
                return Page();
            }

            TempData["Success"] = "User created successfully.";
            return RedirectToPage("./Index");
        }

        private void LoadRoles()
        {
            // Keep this list aligned with what you seed
            RoleOptions = new List<SelectListItem>
            {
                new("User", "User"),
                new("Staff", "Staff"),
                new("Manager", "Manager"),
                new("Admin", "Admin"),
                new("Pending", "Pending")
            };
        }
    }
}