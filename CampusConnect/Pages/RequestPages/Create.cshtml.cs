using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;
using CampusConnect.Data;
using CampusConnect.Models;

namespace CampusConnect.Pages.RequestPages
{
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
            return Page();
        }

        [BindProperty]
        public request request { get; set; } = default!;

        // For more information, see https://aka.ms/RazorPagesCRUD.
        public async Task<IActionResult> OnPostAsync()
        {
            if (!ModelState.IsValid)
            {
                return Page();
            }

            _context.request.Add(request);
            await _context.SaveChangesAsync();

            return RedirectToPage("./Index");
        }
    }
}
