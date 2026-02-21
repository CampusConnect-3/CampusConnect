using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;
using CampusConnect.Data;
using CampusConnect.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;

namespace CampusConnect.Pages.RequestPages
{
    [Authorize]
    public class CreateModel : PageModel
    {
        private readonly CampusConnect.Data.TablesDbContext _context;

        public CreateModel(CampusConnect.Data.TablesDbContext context)
        {
            _context = context;
        }

        public IActionResult OnGet()
        {
            ViewData["assigned_to"] = new SelectList(_context.users, "userID", "email");
            ViewData["categoryID"] = new SelectList(_context.category, "categoryID", "categoryName");
            ViewData["created_by"] = new SelectList(_context.users, "userID", "email");
            ViewData["statusID"] = new SelectList(_context.requestStatus, "statusID", "statusName");
            ViewData["priority"] = new SelectList(new[]
            {
                new { Value = "Low", Text = "Low" },
                new { Value = "Medium", Text = "Medium" },
                new { Value = "High", Text = "High" },
                new { Value = "Critical", Text = "Critical" }
            }, "Value", "Text");
            return Page();
        }

        [BindProperty]
        public request request { get; set; } = default!;

        // For more information, see https://aka.ms/RazorPagesCRUD.
        public async Task<IActionResult> OnPostAsync()
        {
            if (!ModelState.IsValid)
            {
                // Repopulate dropdowns when validation fails
                ViewData["assigned_to"] = new SelectList(_context.users, "userID", "email");
                ViewData["categoryID"] = new SelectList(_context.category, "categoryID", "categoryName");
                ViewData["created_by"] = new SelectList(_context.users, "userID", "email");
                ViewData["statusID"] = new SelectList(_context.requestStatus, "statusID", "statusName");
                ViewData["priority"] = new SelectList(new[]
                {
                    new { Value = "Low", Text = "Low" },
                    new { Value = "Medium", Text = "Medium" },
                    new { Value = "High", Text = "High" },
                    new { Value = "Critical", Text = "Critical" }
                }, "Value", "Text");
                return Page();
            }

            // Resolve the currently authenticated user and assign their userID to created_by.
            // This prevents missing/invalid created_by values and prevents client tampering.
            var currentUserName = User?.Identity?.Name;
            if (string.IsNullOrEmpty(currentUserName))
            {
                ModelState.AddModelError(string.Empty, "Unable to determine the current user. Please sign in again.");
                // Repopulate dropdowns before returning
                ViewData["assigned_to"] = new SelectList(_context.users, "userID", "email");
                ViewData["categoryID"] = new SelectList(_context.category, "categoryID", "categoryName");
                ViewData["created_by"] = new SelectList(_context.users, "userID", "email");
                ViewData["statusID"] = new SelectList(_context.requestStatus, "statusID", "statusName");
                ViewData["priority"] = new SelectList(new[]
                {
                    new { Value = "Low", Text = "Low" },
                    new { Value = "Medium", Text = "Medium" },
                    new { Value = "High", Text = "High" },
                    new { Value = "Critical", Text = "Critical" }
                }, "Value", "Text");
                return Page();
            }

            // Try to find matching user in the application's users table.
            // Using email here because typical Identity setups use email as the Name; adjust to username when appropriate.
            var currentUser = await _context.users.FirstOrDefaultAsync(u => u.email == currentUserName);
            if (currentUser == null)
            {
                ModelState.AddModelError(string.Empty, "Your account is not present in the application's users table. Contact an administrator.");
                ViewData["assigned_to"] = new SelectList(_context.users, "userID", "email");
                ViewData["categoryID"] = new SelectList(_context.category, "categoryID", "categoryName");
                ViewData["created_by"] = new SelectList(_context.users, "userID", "email");
                ViewData["statusID"] = new SelectList(_context.requestStatus, "statusID", "statusName");
                ViewData["priority"] = new SelectList(new[]
                {
                    new { Value = "Low", Text = "Low" },
                    new { Value = "Medium", Text = "Medium" },
                    new { Value = "High", Text = "High" },
                    new { Value = "Critical", Text = "Critical" }
                }, "Value", "Text");
                return Page();
            }

            // Ensure the FK points to an existing user row
            request.created_by = currentUser.userID;

            // Set the timestamp when creating the request
            request.createdAt = DateTime.Now;
            // closedAt remains null until the request is actually closed

            _context.request.Add(request);
            await _context.SaveChangesAsync();

            return RedirectToPage("./Index");
        }
    }
}
