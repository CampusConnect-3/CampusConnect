using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using CampusConnect.Constants;
using CampusConnect.Data;
using CampusConnect.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;

namespace CampusConnect.Pages.RequestPages
{
    [Authorize]
    public class MyRequestsModel : PageModel
    {
        private readonly TablesDbContext _context;

        public MyRequestsModel(TablesDbContext context)
        {
            _context = context;
        }

        [BindProperty(SupportsGet = true)]
        public string? StatusFilter { get; set; }

        public IList<request> Requests { get; set; } = new List<request>();

        public SelectList? StatusOptions { get; set; }

        public async Task<IActionResult> OnGetAsync()
        {
            // Populate status dropdown
            var statuses = await _context.requestStatus
                .OrderBy(s => s.statusName)
                .ToListAsync();

            StatusOptions = new SelectList(statuses, "statusID", "statusName");

            // Resolve current user by email (Identity uses email as username)
            var userEmail = User.Identity?.Name;
            if (string.IsNullOrEmpty(userEmail))
            {
                Requests = new List<request>();
                return Page();
            }

            var appUser = await _context.users
                .AsNoTracking()
                .FirstOrDefaultAsync(u => u.email == userEmail);

            if (appUser == null)
            {
                Requests = new List<request>();
                return Page();
            }

            // Query requests created by this user, include navigation properties
            var q = _context.request
                .AsNoTracking()
                .Include(r => r.status)
                .Include(r => r.category)
                .Where(r => r.created_by == appUser.userID)
                .OrderByDescending(r => r.createdAt)
                .AsQueryable();

            if (!string.IsNullOrEmpty(StatusFilter))
            {
                if (int.TryParse(StatusFilter, out var sid))
                {
                    q = q.Where(r => r.statusID == sid);
                }
                else
                {
                    q = q.Where(r => r.status != null && r.status.statusName == StatusFilter);
                }
            }

            Requests = await q.ToListAsync();

            return Page();
        }
    }
}