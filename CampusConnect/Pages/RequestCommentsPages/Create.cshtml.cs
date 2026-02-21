using CampusConnect.Data;
using CampusConnect.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace CampusConnect.Pages.RequestCommentsPages
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
        ViewData["creatorID"] = new SelectList(_context.users, "userID", "email");
        ViewData["requestID"] = new SelectList(_context.request, "requestID", "buildingName");
            return Page();
        }

        [BindProperty]
        public requestComments requestComments { get; set; } = default!;

        // For more information, see https://aka.ms/RazorPagesCRUD.
        public async Task<IActionResult> OnPostAsync()
        {
            if (!ModelState.IsValid)
            {
                return Page();
            }

            _context.requestComments.Add(requestComments);
            await _context.SaveChangesAsync();

            return RedirectToPage("./Index");
        }
    }
}
