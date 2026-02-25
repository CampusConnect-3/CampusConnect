using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using CampusConnect.Data;
using CampusConnect.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.RazorPages;
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

        public IList<Request> Requests { get; set; } = new List<Request>();

        public async Task OnGetAsync()
        {
            // Get Identity user ID safely
            var identityUserId = User?.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(identityUserId))
            {
                Requests = new List<Request>();
                return;
            }

            // Resolve legacy user row
            var appUser = await _context.users
                .AsNoTracking()
                .FirstOrDefaultAsync(u => u.IdentityUserId == identityUserId);

            if (appUser == null)
            {
                Requests = new List<Request>();
                return;
            }

            // Build query explicitly (NO shadow FKs)
            var q = _context.request
                .AsNoTracking()
                .Where(r => r.created_by == appUser.userID)
                .Include(r => r.category)
                .Include(r => r.status)
                .Include(r => r.assignedTo);

            Requests = await q.ToListAsync();
        }
    }
}