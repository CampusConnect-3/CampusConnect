using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using CampusConnect.Data;
using CampusConnect.Models;

namespace CampusConnect.Pages.UserRolesPages
{
    public class IndexModel : PageModel
    {
        private readonly CampusConnect.Data.TablesDbContext _context;

        public IndexModel(CampusConnect.Data.TablesDbContext context)
        {
            _context = context;
        }

        public IList<userRoles> userRoles { get;set; } = default!;

        public async Task OnGetAsync()
        {
            userRoles = await _context.userRoles
                .Include(u => u.role)
                .Include(u => u.user).ToListAsync();
        }
    }
}
