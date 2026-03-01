using CampusConnect.Data;
using CampusConnect.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace CampusConnect.Pages.RequestPages
{
    [Authorize(Roles = "Admin,Manager,Staff,User")]
    public class DetailsModel : PageModel
    {
        private readonly TablesDbContext _context;

        public DetailsModel(TablesDbContext context)
        {
            _context = context;
        }

        public request request { get; set; } = default!;
        public List<attachments> Attachments { get; set; } = new();

        public async Task<IActionResult> OnGetAsync(int? id, CancellationToken cancellationToken = default)
        {
            if (id == null)
                return NotFound();

            var req = await _context.request
                .Include(r => r.createdBy)
                .Include(r => r.assignedTo)
                .Include(r => r.status)
                .Include(r => r.category)
                .FirstOrDefaultAsync(m => m.requestID == id, cancellationToken);

            if (req == null)
            {
                return NotFound();
            }

            request = req;

            // Load attachments for this request
            Attachments = await _context.attachments
                .Where(a => a.requestID == id)
                .Include(a => a.creator)
                .OrderByDescending(a => a.uploadedAt)
                .ToListAsync(cancellationToken);

            return Page();
        }
    }
}