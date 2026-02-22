using CampusConnect.Data;
using CampusConnect.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace CampusConnect.Pages.RequestAttachmentsPages
{
    [Authorize(Roles = "Admin,Manager")]
    public class EditModel : PageModel
    {
        private readonly CampusConnect.Data.TablesDbContext _context;

        public EditModel(CampusConnect.Data.TablesDbContext context)
        {
            _context = context;
        }

        [BindProperty]
        public attachments attachments { get; set; } = default!;

        public async Task<IActionResult> OnGetAsync(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var attachments =  await _context.attachments.FirstOrDefaultAsync(m => m.fileID == id);
            if (attachments == null)
            {
                return NotFound();
            }
            attachments = attachments;
           ViewData["creatorID"] = new SelectList(_context.users, "userID", "email");
           ViewData["requestID"] = new SelectList(_context.request, "requestID", "buildingName");
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

            _context.Attach(attachments).State = EntityState.Modified;

            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!attachmentsExists(attachments.fileID))
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

        private bool attachmentsExists(int id)
        {
            return _context.attachments.Any(e => e.fileID == id);
        }
    }
}
