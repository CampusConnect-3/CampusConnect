using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using CampusConnect.Data;
using CampusConnect.Models;

namespace CampusConnect.Pages.RequestCommentsPages
{
    public class IndexModel : PageModel
    {
        private readonly CampusConnect.Data.TablesDbContext _context;

        public IndexModel(CampusConnect.Data.TablesDbContext context)
        {
            _context = context;
        }

        public IList<requestComments> requestComments { get;set; } = default!;

        public async Task OnGetAsync()
        {
            requestComments = await _context.requestComments
                .Include(r => r.creator)
                .Include(r => r.request).ToListAsync();
        }
    }
}
