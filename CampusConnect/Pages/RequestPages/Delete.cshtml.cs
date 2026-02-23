using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using CampusConnect.Data;
using CampusConnect.Models;
using Microsoft.Extensions.Logging;
using System.Security.Claims;

namespace CampusConnect.Pages.RequestPages
{
    public class DeleteModel : PageModel
    {
        private readonly CampusConnect.Data.TablesDbContext _context;

        private readonly ILogger<DeleteModel> _logger;
        public DeleteModel(TablesDbContext context, ILogger<DeleteModel> logger)
        {
            _context = context;
            _logger = logger;
        }

        [BindProperty]
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

        public async Task<IActionResult> OnPostAsync(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var request = await _context.request.FindAsync(id);
            if (request != null)
            {
                request = request;
                _context.request.Remove(request);
                await _context.SaveChangesAsync();

                _logger.LogWarning("CRITICAL: Request deleted. RequestId={RequestId} UserId={UserId} TraceId={TraceId}",
                    id,User.FindFirst(ClaimTypes.NameIdentifier)?.Value,
                    HttpContext.TraceIdentifier 
                );
            }

            return RedirectToPage("./Index");
        }
    }
}
