using CampusConnect.Data;
using CampusConnect.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using System.Threading.Tasks;

namespace CampusConnect.Pages.RequestPages
{
    [Authorize]
    public class DetailsModel : PageModel
    {
        private readonly TablesDbContext _context;

        public DetailsModel(TablesDbContext context)
        {
            _context = context;
        }

        public Request request { get; set; } = default!;

        public async Task<IActionResult> OnGetAsync(int? id)
        {
            if (id == null)
                return NotFound();

            var found = await _context.request.FirstOrDefaultAsync(m => m.requestID == id);
            if (found == null)
                return NotFound();

            // Assign to PageModel property so Razor can access it
            request = found;

            return Page();
        }
    }
}