using CampusConnect.Constants;
using CampusConnect.Data;
using CampusConnect.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace CampusConnect.Pages.Manager
{
    [Authorize(Roles = "Manager")]
    public class ReviewRequestsModel : PageModel
    {
        private readonly TablesDbContext _context;

        public ReviewRequestsModel(TablesDbContext context)
        {
            _context = context;
        }

        public IList<request> Requests { get; set; } = new List<request>();

        [BindProperty]
        public int RequestId { get; set; }

        private async Task LoadPageAsync()
        {
            // CHANGED: Show only "Completed" requests awaiting manager review
            Requests = await _context.request
                .AsNoTracking()
                .Include(r => r.createdBy)
                .Include(r => r.assignedTo)
                .Include(r => r.status)
                .Include(r => r.category)
                .Where(r =>
                    r.status != null &&
                    r.status.statusName == RequestStatuses.Completed) // CHANGED from InProgress
                .OrderByDescending(r => r.createdAt)
                .ToListAsync();
        }

        public async Task OnGetAsync()
        {
            await LoadPageAsync();
        }

        public async Task<IActionResult> OnPostCloseAsync()
        {
            var req = await _context.request
                .Include(r => r.status)
                .FirstOrDefaultAsync(r => r.requestID == RequestId);

            if (req == null)
                return NotFound();

            var closedStatus = await _context.requestStatus
                .FirstOrDefaultAsync(s => s.statusName == RequestStatuses.Closed);

            if (closedStatus == null)
            {
                ModelState.AddModelError(string.Empty, "Closed status was not found in the database.");
                await LoadPageAsync();
                return Page();
            }

            // Move from "Completed" to "Closed" (archive)
            req.statusID = closedStatus.statusID;
            
            // closedAt should already be set from when staff marked it completed
            // But ensure it's set if somehow missing
            if (!req.closedAt.HasValue)
            {
                req.closedAt = DateTime.Now;
            }

            await _context.SaveChangesAsync();

            return RedirectToPage();
        }
    }
}