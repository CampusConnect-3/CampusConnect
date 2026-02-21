using CampusConnect.Data;
using CampusConnect.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace CampusConnect.Pages.RequestAttachmentsPages
{
    [Authorize]
    public class DetailsModel : PageModel
    {
        private readonly CampusConnect.Data.TablesDbContext _context;

        public DetailsModel(CampusConnect.Data.TablesDbContext context)
        {
            _context = context;
        }

        public attachments attachments { get; set; } = default!;

        public async Task<IActionResult> OnGetAsync(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var attachments = await _context.attachments.FirstOrDefaultAsync(m => m.fileID == id);
            if (attachments == null)
            {
                return NotFound();
            }
            else
            {
                attachments = attachments;
            }
            return Page();
        }
    }
}
