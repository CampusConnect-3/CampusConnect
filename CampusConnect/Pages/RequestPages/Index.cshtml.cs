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

namespace CampusConnect.Pages.RequestPages
{
    [Authorize]
    public class IndexModel : PageModel
    {
        private readonly CampusConnect.Data.TablesDbContext _context;

        public IndexModel(CampusConnect.Data.TablesDbContext context)
        {
            _context = context;
        }

        public IList<request> request { get;set; } = default!;

        public async Task OnGetAsync()
        {
            request = await _context.request
                .Include(r => r.assignedTo)
                .Include(r => r.category)
                .Include(r => r.createdBy)
                .Include(r => r.status).ToListAsync();
        }
    }
}
