using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace CampusConnect.Pages
{
    [Authorize(Roles = "Pending")]
    public class PendingApprovalModel : PageModel
    {
        public void OnGet()
        {
        }
    }
}