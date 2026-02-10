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

namespace CampusConnect.Pages.UserRolesPages
{
    public class EditModel : PageModel
    {
        private readonly CampusConnect.Data.TablesDbContext _context;

        public EditModel(CampusConnect.Data.TablesDbContext context)
        {
            _context = context;
        }

        [BindProperty]
        public userRoles userRoles { get; set; } = default!;

        public async Task<IActionResult> OnGetAsync(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var userroles =  await _context.userRoles.FirstOrDefaultAsync(m => m.roleID == id);
            if (userroles == null)
            {
                return NotFound();
            }
            userRoles = userroles;
           ViewData["roleID"] = new SelectList(_context.roles, "roleID", "roleName");
           ViewData["userID"] = new SelectList(_context.users, "userID", "email");
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

            _context.Attach(userRoles).State = EntityState.Modified;

            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!userRolesExists(userRoles.roleID))
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

        private bool userRolesExists(int id)
        {
            return _context.userRoles.Any(e => e.roleID == id);
        }
    }
}
