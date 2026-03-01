using CampusConnect.Data;
using CampusConnect.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace CampusConnect.Pages.RequestPages
{
    // Request Queue (Admin/Manager/Staff ONLY)
    [Authorize(Roles = "Admin,Manager,Staff")]
    public class IndexModel : PageModel
    {
        private readonly TablesDbContext _context;

        public IndexModel(TablesDbContext context)
        {
            _context = context;
        }

        // The Razor page expects this list name
        public IList<request> request { get; set; } = new List<request>();

        public async Task OnGetAsync(CancellationToken cancellationToken = default)
        {
            request = await _context.request
                .AsNoTracking()
                .Include(r => r.assignedTo)
                .Include(r => r.category)
                .Include(r => r.createdBy)
                .Include(r => r.status)
                .OrderByDescending(r => r.createdAt)
                .ToListAsync(cancellationToken);
        }
    }
}