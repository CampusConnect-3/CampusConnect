using CampusConnect.Data;
using CampusConnect.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using System.Threading.Tasks;

namespace CampusConnect.Pages.Admin.UserRolesPages
{
    [Authorize(Roles = "Admin")]
    public class DetailsModel : PageModel
    {
        private readonly TablesDbContext _context;

        public DetailsModel(TablesDbContext context)
        {
            _context = context;
        }

        public userRoles userRoles { get; set; } = default!;

        public async Task<IActionResult> OnGetAsync(int? roleID, int? userID)
        {
            if (roleID == null || userID == null)
                return NotFound();

            var ur = await _context.userRoles
                .AsNoTracking()
                .Include(x => x.role)
                .Include(x => x.user)
                .FirstOrDefaultAsync(x => x.roleID == roleID && x.userID == userID);

            if (ur == null)
                return NotFound();

            userRoles = ur;
            return Page();
        }
    }
}