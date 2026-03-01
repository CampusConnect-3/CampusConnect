//using CampusConnect.Data;
//using CampusConnect.Models;
//using Microsoft.AspNetCore.Authorization;
//using Microsoft.AspNetCore.Mvc.RazorPages;
//using Microsoft.EntityFrameworkCore;
//using System.Security.Claims;

//namespace CampusConnect.Pages
//{
//    [Authorize(Roles = "User")]
//    public class MyRequestsModel : PageModel
//    {
//        private readonly TablesDbContext _context;

//        public MyRequestsModel(TablesDbContext context)
//        {
//            _context = context;
//        }

//        public List<request> MyRequests { get; set; } = new();

//        public async Task OnGetAsync()
//        {
//            var identityUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);

//            if (string.IsNullOrEmpty(identityUserId))
//                return;

//            var appUser = await _context.users
//                .AsNoTracking()
//                .FirstOrDefaultAsync(u => u.identityUserId == identityUserId);

//            if (appUser == null)
//                return;

//            MyRequests = await _context.request
//                .AsNoTracking()
//                .Include(r => r.status)
//                .Where(r => r.created_by == appUser.userID)
//                .OrderByDescending(r => r.createdAt)
//                .ToListAsync();
//        }
//    }
//}