using CampusConnect.Constants;
using CampusConnect.Data;
using CampusConnect.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;
using System.Globalization;
using System.Text;

namespace CampusConnect.Pages.Manager
{
    [Authorize(Roles = "Manager")]
    public class ArchiveModel : PageModel
    {
        private readonly TablesDbContext _context;

        public ArchiveModel(TablesDbContext context)
        {
            _context = context;
        }

        public IList<request> ClosedRequests { get; set; } = new List<request>();

        [BindProperty(SupportsGet = true)]
        public string? SearchTerm { get; set; }

        [BindProperty(SupportsGet = true)]
        public string? PriorityFilter { get; set; }

        [BindProperty(SupportsGet = true)]
        [DataType(DataType.Date)]
        public DateTime? DateFrom { get; set; }

        [BindProperty(SupportsGet = true)]
        [DataType(DataType.Date)]
        public DateTime? DateTo { get; set; }

        public List<string> PriorityOptions { get; set; } = new()
        {
            "Low",
            "Medium",
            "High",
            "Critical"
        };

        private IQueryable<request> BuildQuery()
        {
            var query = _context.request
                .AsNoTracking()
                .Include(r => r.createdBy)
                .Include(r => r.assignedTo)
                .Include(r => r.status)
                .Include(r => r.category)
                .Where(r => r.status != null && r.status.statusName == RequestStatuses.Closed);

            if (!string.IsNullOrWhiteSpace(SearchTerm))
            {
                var term = SearchTerm.Trim();

                query = query.Where(r =>
                    r.title.Contains(term) ||
                    (r.createdBy != null && r.createdBy.email.Contains(term)) ||
                    (r.assignedTo != null && r.assignedTo.email.Contains(term)) ||
                    (r.category != null && r.category.categoryName.Contains(term)));
            }

            if (!string.IsNullOrWhiteSpace(PriorityFilter))
            {
                query = query.Where(r => r.priority == PriorityFilter);
            }

            if (DateFrom.HasValue)
            {
                var fromDate = DateFrom.Value.Date;
                query = query.Where(r => r.closedAt.HasValue && r.closedAt.Value.Date >= fromDate);
            }

            if (DateTo.HasValue)
            {
                var toDate = DateTo.Value.Date;
                query = query.Where(r => r.closedAt.HasValue && r.closedAt.Value.Date <= toDate);
            }

            return query.OrderByDescending(r => r.closedAt);
        }

        public async Task OnGetAsync()
        {
            ClosedRequests = await BuildQuery().ToListAsync();
        }

        public async Task<IActionResult> OnGetExportCsvAsync()
        {
            var data = await BuildQuery().ToListAsync();

            var sb = new StringBuilder();
            sb.AppendLine("RequestID,Title,CreatedBy,AssignedTo,Category,Priority,Status,CreatedAt,ClosedAt");

            foreach (var item in data)
            {
                static string CsvEscape(string? value)
                {
                    value ??= string.Empty;
                    value = value.Replace("\"", "\"\"");
                    return $"\"{value}\"";
                }

                sb.AppendLine(string.Join(",",
                    item.requestID,
                    CsvEscape(item.title),
                    CsvEscape(item.createdBy?.email),
                    CsvEscape(item.assignedTo?.email),
                    CsvEscape(item.category?.categoryName),
                    CsvEscape(item.priority),
                    CsvEscape(item.status?.statusName),
                    CsvEscape(item.createdAt.ToString("MM/dd/yyyy", CultureInfo.InvariantCulture)),
                    CsvEscape(item.closedAt?.ToString("MM/dd/yyyy", CultureInfo.InvariantCulture))
                ));
            }

            var bytes = Encoding.UTF8.GetBytes(sb.ToString());
            var fileName = $"archive_export_{DateTime.Now:yyyyMMdd_HHmmss}.csv";

            return File(bytes, "text/csv", fileName);
        }
    }
}