using CampusConnect.Data;
using CampusConnect.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace CampusConnect.Pages.Admin.UserRolesPages
{
    [Authorize(Roles = "Admin")]
    public class IndexModel : PageModel
    {
        private readonly TablesDbContext _context;

        public IndexModel(TablesDbContext context)
        {
            _context = context;
        }

        public IList<userRoles> userRoles { get; set; } = default!;

        public async Task OnGetAsync()
        {
            userRoles = await _context.userRoles
                .AsNoTracking()
                .Include(u => u.role)
                .Include(u => u.user)
                .ToListAsync();
        }
    }
}