using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;
using CampusConnect.Data;
using CampusConnect.Models.MongoDB;
using CampusConnect.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using MongoDB.Bson;

namespace CampusConnect.Areas.Identity.Pages.Account
{
    [Authorize]
    public class FirstLoginPasswordChangeModel : PageModel
    {
        private readonly UserManager<IdentityUser> _userManager;
        private readonly SignInManager<IdentityUser> _signInManager;
        private readonly TablesDbContext _tablesDb;
        private readonly MongoDBService _mongoService;
        private readonly ILogger<FirstLoginPasswordChangeModel> _logger;

        public FirstLoginPasswordChangeModel(
            UserManager<IdentityUser> userManager,
            SignInManager<IdentityUser> signInManager,
            TablesDbContext tablesDb,
            MongoDBService mongoService,
            ILogger<FirstLoginPasswordChangeModel> logger)
        {
            _userManager = userManager;
            _signInManager = signInManager;
            _tablesDb = tablesDb;
            _mongoService = mongoService;
            _logger = logger;
        }

        [BindProperty]
        public InputModel Input { get; set; } = new();

        [TempData]
        public string? StatusMessage { get; set; }

        public class InputModel
        {
            [Required]
            [DataType(DataType.Password)]
            [Display(Name = "Current Password")]
            public string OldPassword { get; set; } = "";

            [Required]
            [StringLength(100, ErrorMessage = "The {0} must be at least {2} and at max {1} characters long.", MinimumLength = 6)]
            [DataType(DataType.Password)]
            [Display(Name = "New Password")]
            public string NewPassword { get; set; } = "";

            [DataType(DataType.Password)]
            [Display(Name = "Confirm New Password")]
            [Compare("NewPassword", ErrorMessage = "The new password and confirmation password do not match.")]
            public string ConfirmPassword { get; set; } = "";
        }

        public async Task<IActionResult> OnGetAsync()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                return NotFound($"Unable to load user with ID '{_userManager.GetUserId(User)}'.");
            }

            // Check if user actually needs to change password
            var appUser = await _tablesDb.users.FirstOrDefaultAsync(u => u.identityUserId == user.Id);
            if (appUser == null || !appUser.RequirePasswordChange)
            {
                // User doesn't need to change password, redirect to home
                return RedirectToPage("/Index");
            }

            return Page();
        }

        public async Task<IActionResult> OnPostAsync()
        {
            if (!ModelState.IsValid)
            {
                return Page();
            }

            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                return NotFound($"Unable to load user with ID '{_userManager.GetUserId(User)}'.");
            }

            var changePasswordResult = await _userManager.ChangePasswordAsync(user, Input.OldPassword, Input.NewPassword);
            if (!changePasswordResult.Succeeded)
            {
                foreach (var error in changePasswordResult.Errors)
                {
                    ModelState.AddModelError(string.Empty, error.Description);
                }
                return Page();
            }

            // Clear the RequirePasswordChange flag
            var appUser = await _tablesDb.users.FirstOrDefaultAsync(u => u.identityUserId == user.Id);
            if (appUser != null)
            {
                appUser.RequirePasswordChange = false;
                await _tablesDb.SaveChangesAsync();
            }

            // Log the password change
            var roles = await _userManager.GetRolesAsync(user);
            await _mongoService.LogActivityAsync(new ActivityLog
            {
                UserId = user.Id,
                UserName = user.Email ?? "Unknown",
                UserRole = roles.FirstOrDefault() ?? "User",
                Action = "first_login_password_change",
                RequestId = null,
                Details = new BsonDocument
                {
                    { "userAgent", Request.Headers["User-Agent"].ToString() }
                },
                IpAddress = HttpContext.Connection.RemoteIpAddress?.ToString()
            });

            await _signInManager.RefreshSignInAsync(user);
            _logger.LogInformation("User changed their password successfully on first login.");
            StatusMessage = "Your password has been changed successfully.";

            return RedirectToPage("/Index");
        }
    }
}