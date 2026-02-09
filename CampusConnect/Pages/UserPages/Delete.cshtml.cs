using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using CampusConnect.Data;
using CampusConnect.Models;

namespace CampusConnect.Pages.UserPages
{
    public class DeleteModel : PageModel
    {
        private readonly CampusConnect.Data.TablesDbContext _context;

        public DeleteModel(CampusConnect.Data.TablesDbContext context)
        {
            _context = context;
        }

        [BindProperty]
        public user user { get; set; } = default!;

        public async Task<IActionResult> OnGetAsync(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var user = await _context.users.FirstOrDefaultAsync(m => m.userID == id);

            if (user == null)
            {
                return NotFound();
            }
            else
            {
                user = user;
            }
            return Page();
        }

        public async Task<IActionResult> OnPostAsync(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var user = await _context.users.FindAsync(id);
            if (user != null)
            {
                user = user;
                _context.users.Remove(user);
                await _context.SaveChangesAsync();
            }

            return RedirectToPage("./Index");
        }
    }
}
