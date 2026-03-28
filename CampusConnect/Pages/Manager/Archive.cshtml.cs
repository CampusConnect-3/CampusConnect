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
        [DataType(DataType.Date)]
        public DateTime? DateFrom { get; set; }

        [BindProperty(SupportsGet = true)]
        [DataType(DataType.Date)]
        public DateTime? DateTo { get; set; }

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
                    (r.createdBy != null &&
                        ((r.createdBy.fName + " " + r.createdBy.lName).Contains(term) ||
                         r.createdBy.email.Contains(term))) ||
                    (r.assignedTo != null &&
                        ((r.assignedTo.fName + " " + r.assignedTo.lName).Contains(term) ||
                         r.assignedTo.email.Contains(term))) ||
                    (r.category != null && r.category.categoryName.Contains(term)));
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
            sb.AppendLine("TicketNumber,Title,CreatedBy,AssignedTo,Category,Priority,Status,CreatedAt,ClosedAt");

            foreach (var item in data)
            {
                static string CsvEscape(string? value)
                {
                    value ??= string.Empty;
                    value = value.Replace("\"", "\"\"");
                    return $"\"{value}\"";
                }

                var createdByName = item.createdBy != null
                    ? $"{item.createdBy.fName} {item.createdBy.lName}".Trim()
                    : "Unknown";

                var assignedToName = item.assignedTo != null
                    ? $"{item.assignedTo.fName} {item.assignedTo.lName}".Trim()
                    : "Unassigned";

                sb.AppendLine(string.Join(",",
                    item.requestID,
                    CsvEscape(item.title),
                    CsvEscape(createdByName),
                    CsvEscape(assignedToName),
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

        public async Task<IActionResult> OnGetExportSingleCsvAsync(int id)
        {
            var item = await _context.request
                .AsNoTracking()
                .Include(r => r.createdBy)
                .Include(r => r.assignedTo)
                .Include(r => r.status)
                .Include(r => r.category)
                .FirstOrDefaultAsync(r => r.requestID == id);

            if (item == null)
                return NotFound();

            static string CsvEscape(string? value)
            {
                value ??= string.Empty;
                value = value.Replace("\"", "\"\"");
                return $"\"{value}\"";
            }

            var createdByName = item.createdBy != null
                ? $"{item.createdBy.fName} {item.createdBy.lName}".Trim()
                : "Unknown";

            var assignedToName = item.assignedTo != null
                ? $"{item.assignedTo.fName} {item.assignedTo.lName}".Trim()
                : "Unassigned";

            var sb = new StringBuilder();
            sb.AppendLine("Field,Value");
            sb.AppendLine($"\"Ticket Number\",{CsvEscape(item.requestID.ToString())}");
            sb.AppendLine($"\"Title\",{CsvEscape(item.title)}");
            sb.AppendLine($"\"Description\",{CsvEscape(item.description)}");
            sb.AppendLine($"\"Created By\",{CsvEscape(createdByName)}");
            sb.AppendLine($"\"Assigned To\",{CsvEscape(assignedToName)}");
            sb.AppendLine($"\"Category\",{CsvEscape(item.category?.categoryName)}");
            sb.AppendLine($"\"Priority\",{CsvEscape(item.priority)}");
            sb.AppendLine($"\"Status\",{CsvEscape(item.status?.statusName)}");
            sb.AppendLine($"\"Building\",{CsvEscape(item.buildingName)}");
            sb.AppendLine($"\"Room\",{CsvEscape(item.roomNumber)}");
            sb.AppendLine($"\"Phone\",{CsvEscape(item.phoneNumber)}");
            sb.AppendLine($"\"Email\",{CsvEscape(item.email)}");
            sb.AppendLine($"\"Created At\",{CsvEscape(item.createdAt.ToString("MM/dd/yyyy", CultureInfo.InvariantCulture))}");
            sb.AppendLine($"\"Closed At\",{CsvEscape(item.closedAt?.ToString("MM/dd/yyyy", CultureInfo.InvariantCulture))}");

            var bytes = Encoding.UTF8.GetBytes(sb.ToString());
            var fileName = $"ticket_{item.requestID}_details.csv";

            return File(bytes, "text/csv", fileName);
        }
    }
}