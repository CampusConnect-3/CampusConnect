using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using CampusConnect.Data;
using CampusConnect.Models;

namespace CampusConnect.Pages.RequestPages
{
    public class DetailsModel : PageModel
    {
        private readonly CampusConnect.Data.TablesDbContext _context;

        public DetailsModel(CampusConnect.Data.TablesDbContext context)
        {
            _context = context;
        }

        public request request { get; set; } = default!;

        public async Task<IActionResult> OnGetAsync(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var request = await _context.request.FirstOrDefaultAsync(m => m.requestID == id);
            if (request == null)
            {
                return NotFound();
            }
            else
            {
                request = request;
            }
            return Page();
        }
    }
}
