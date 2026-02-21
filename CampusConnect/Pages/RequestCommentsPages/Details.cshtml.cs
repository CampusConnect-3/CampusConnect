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

namespace CampusConnect.Pages.RequestCommentsPages
{
    [Authorize]
    public class DetailsModel : PageModel
    {
        private readonly CampusConnect.Data.TablesDbContext _context;

        public DetailsModel(CampusConnect.Data.TablesDbContext context)
        {
            _context = context;
        }

        public requestComments requestComments { get; set; } = default!;

        public async Task<IActionResult> OnGetAsync(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var requestcomments = await _context.requestComments.FirstOrDefaultAsync(m => m.commentID == id);
            if (requestcomments == null)
            {
                return NotFound();
            }
            else
            {
                requestComments = requestcomments;
            }
            return Page();
        }
    }
}
