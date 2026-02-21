using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using CampusConnect.Data;
using CampusConnect.Models;
using Microsoft.AspNetCore.Authorization;

namespace CampusConnect.Pages.RequestPages
{
    [Authorize(Roles= "Admin,Manager")]
    public class EditModel : PageModel
    {
        private readonly CampusConnect.Data.TablesDbContext _context;

        public EditModel(CampusConnect.Data.TablesDbContext context)
        {
            _context = context;
        }

        [BindProperty]
        public request request { get; set; } = default!;

        public async Task<IActionResult> OnGetAsync(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var request =  await _context.request.FirstOrDefaultAsync(m => m.requestID == id);
            if (request == null)
            {
                return NotFound();
            }
            request = request;
           ViewData["assigned_to"] = new SelectList(_context.users, "userID", "email");
           ViewData["categoryID"] = new SelectList(_context.category, "categoryID", "categoryName");
           ViewData["created_by"] = new SelectList(_context.users, "userID", "email");
           ViewData["statusID"] = new SelectList(_context.requestStatus, "statusID", "statusName");
            return Page();
        }

        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more information, see https://aka.ms/RazorPagesCRUD.
        public async Task<IActionResult> OnPostAsync()
        {
            if (!ModelState.IsValid)
            {
                return Page();
            }

            _context.Attach(request).State = EntityState.Modified;

            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!requestExists(request.requestID))
                {
                    return NotFound();
                }
                else
                {
                    throw;
                }
            }

            return RedirectToPage("./Index");
        }

        private bool requestExists(int id)
        {
            return _context.request.Any(e => e.requestID == id);
        }
    }
}
