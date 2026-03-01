using CampusConnect.Data;
using CampusConnect.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using System.Threading.Tasks;

namespace CampusConnect.Pages.Admin.UserPages
{
    [Authorize(Roles = "Admin")]
    public class DetailsModel : PageModel
    {
        private readonly TablesDbContext _context;

        public DetailsModel(TablesDbContext context)
        {
            _context = context;
        }

        public user user { get; set; } = default!;

        public async Task<IActionResult> OnGetAsync(int? id)
        {
            if (id == null)
                return NotFound();

            var u = await _context.users
                .AsNoTracking()
                .FirstOrDefaultAsync(m => m.userID == id);

            if (u == null)
                return NotFound();

            user = u;
            return Page();
        }
    }
}