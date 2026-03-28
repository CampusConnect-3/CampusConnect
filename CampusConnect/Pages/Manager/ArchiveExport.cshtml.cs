using CampusConnect.Constants;
using CampusConnect.Data;
using CampusConnect.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using NuGet.Packaging;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace CampusConnect.Pages.Manager
{
    [Authorize(Roles = "Manager")]
    public class ArchiveExportModel : PageModel
    {
        private readonly TablesDbContext _context;

        public ArchiveExportModel(TablesDbContext context)
        {
            _context = context;
        }

        public request RequestItem { get; set; } = default!;

        public async Task<IActionResult> OnGetAsync(int? id)
        {
            if (id == null)
                return NotFound();

            var item = await _context.request
                .AsNoTracking()
                .Include(r => r.createdBy)
                .Include(r => r.assignedTo)
                .Include(r => r.status)
                .Include(r => r.category)
                .FirstOrDefaultAsync(r =>
                    r.requestID == id &&
                    r.status != null &&
                    r.status.statusName == RequestStatuses.Closed);

            if (item == null)
                return NotFound();

            RequestItem = item;
            return Page();
        }

        public async Task<IActionResult> OnGetExportPdfAsync(int? id)
        {
            if (id == null)
                return NotFound();

            var item = await _context.request
                .AsNoTracking()
                .Include(r => r.createdBy)
                .Include(r => r.assignedTo)
                .Include(r => r.status)
                .Include(r => r.category)
                .FirstOrDefaultAsync(r =>
                    r.requestID == id &&
                    r.status != null &&
                    r.status.statusName == RequestStatuses.Closed);

            if (item == null)
                return NotFound();

            var createdByName = item.createdBy != null
                ? $"{item.createdBy.fName} {item.createdBy.lName}".Trim()
                : "Unknown";

            var assignedToName = item.assignedTo != null
                ? $"{item.assignedTo.fName} {item.assignedTo.lName}".Trim()
                : "Unassigned";

            QuestPDF.Settings.License = QuestPDF.Infrastructure.LicenseType.Community;

            var pdfBytes = Document.Create(container =>
            {
                container.Page(page =>
                {
                    page.Margin(30);
                    page.Size(PageSizes.A4);
                    page.DefaultTextStyle(x => x.FontSize(11));

                    page.Header()
                        .Column(column =>
                        {
                            column.Item().Text("CampusConnect")
                                .FontSize(20)
                                .Bold();

                            column.Item().Text("Service Request Report")
                                .FontSize(16)
                                .SemiBold();

                            column.Item().Text($"Generated: {DateTime.Now:MM/dd/yyyy hh:mm tt}");
                        });

                    page.Content()
                        .PaddingVertical(15)
                        .Column(column =>
                        {
                            column.Spacing(12);

                            // Ticket Summary
                            column.Item().Border(1).Padding(10).Column(summary =>
                            {
                                summary.Item().Text("Ticket Summary").Bold().FontSize(14);
                                summary.Item().Text($"Ticket Number: {item.requestID}");
                                summary.Item().Text($"Title: {item.title}");
                                summary.Item().Text($"Status: {item.status?.statusName}");
                                summary.Item().Text($"Priority: {item.priority}");
                                summary.Item().Text($"Category: {item.category?.categoryName}");
                            });

                            // Request Details
                            column.Item().Border(1).Padding(10).Column(details =>
                            {
                                details.Item().Text("Request Details").Bold().FontSize(14);
                                details.Item().Text($"Description: {item.description}");
                                details.Item().Text($"Building: {item.buildingName}");
                                details.Item().Text($"Room Number: {item.roomNumber}");
                                details.Item().Text($"Phone Number: {item.phoneNumber}");
                                details.Item().Text($"Email: {item.email}");
                            });

                            // People
                            column.Item().Border(1).Padding(10).Column(people =>
                            {
                                people.Item().Text("People Involved").Bold().FontSize(14);
                                people.Item().Text($"Created By: {createdByName}");
                                people.Item().Text($"Assigned To: {assignedToName}");
                            });

                            // Timeline
                            column.Item().Border(1).Padding(10).Column(timeline =>
                            {
                                timeline.Item().Text("Timeline").Bold().FontSize(14);
                                timeline.Item().Text($"Created At: {item.createdAt:MM/dd/yyyy}");
                                timeline.Item().Text($"Closed At: {item.closedAt:MM/dd/yyyy}");
                            });
                        });

                    page.Footer()
                        .AlignCenter()
                        .Text("Generated by CampusConnect")
                        .FontSize(10)
                        .Italic();
                });
            }).GeneratePdf();

            var fileName = $"ticket_{item.requestID}_report.pdf";
            return File(pdfBytes, "application/pdf", fileName);
        }
    }
}