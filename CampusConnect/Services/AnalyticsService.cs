using CampusConnect.Data;
using CampusConnect.Models;
using Microsoft.EntityFrameworkCore;

namespace CampusConnect.Services
{
    public class AnalyticsService
    {
        private readonly TablesDbContext _context;

        public AnalyticsService(TablesDbContext context)
        {
            _context = context;
        }

        public async Task<AnalyticsDashboardData> GetDashboardDataAsync(DateTime? startDate = null, DateTime? endDate = null)
        {
            // Default to last 30 days if no dates provided
            startDate ??= DateTime.Now.AddDays(-30);
            endDate ??= DateTime.Now;

            var requests = await _context.request
                .Include(r => r.category)
                .Include(r => r.status)
                .Include(r => r.createdBy)
                .Include(r => r.assignedTo)
                .Where(r => r.createdAt >= startDate && r.createdAt <= endDate)
                .ToListAsync();

            return new AnalyticsDashboardData
            {
                TotalRequests = requests.Count,
                PendingRequests = requests.Count(r => r.status?.statusName == "Pending"),
                InProgressRequests = requests.Count(r => r.status?.statusName == "In Progress"),
                CompletedRequests = requests.Count(r => r.status?.statusName == "Completed"),
                CancelledRequests = requests.Count(r => r.status?.statusName == "Cancelled"),

                AverageResolutionTimeHours = CalculateAverageResolutionTime(requests),

                RequestsByCategory = requests
                    .GroupBy(r => r.category?.categoryName ?? "Uncategorized")
                    .Select(g => new KeyValuePair<string, int>(g.Key, g.Count()))
                    .OrderByDescending(kv => kv.Value)
                    .ToList(),

                RequestsByPriority = requests
                    .GroupBy(r => r.priority)
                    .Select(g => new KeyValuePair<string, int>(g.Key, g.Count()))
                    .OrderByDescending(kv => kv.Value)
                    .ToList(),

                RequestsByStatus = requests
                    .GroupBy(r => r.status?.statusName ?? "Unknown")
                    .Select(g => new KeyValuePair<string, int>(g.Key, g.Count()))
                    .ToList(),

                DailyRequestTrend = GetDailyTrend(requests, startDate.Value, endDate.Value),

                TopRequesters = requests
                    .GroupBy(r => r.createdBy != null ? $"{r.createdBy.fName} {r.createdBy.lName}" : "Unknown")
                    .Select(g => new KeyValuePair<string, int>(g.Key, g.Count()))
                    .OrderByDescending(kv => kv.Value)
                    .Take(10)
                    .ToList(),

                StartDate = startDate.Value,
                EndDate = endDate.Value
            };
        }

        private double CalculateAverageResolutionTime(List<request> requests)
        {
            var completedRequests = requests.Where(r => r.closedAt.HasValue).ToList();

            if (!completedRequests.Any())
                return 0;

            var totalHours = completedRequests
                .Select(r => (r.closedAt!.Value - r.createdAt).TotalHours)
                .Sum();

            return Math.Round(totalHours / completedRequests.Count, 2);
        }

        private List<KeyValuePair<string, int>> GetDailyTrend(List<request> requests, DateTime startDate, DateTime endDate)
        {
            var dailyData = new List<KeyValuePair<string, int>>();

            for (var date = startDate.Date; date <= endDate.Date; date = date.AddDays(1))
            {
                var count = requests.Count(r => r.createdAt.Date == date);
                dailyData.Add(new KeyValuePair<string, int>(date.ToString("MM/dd"), count));
            }

            return dailyData;
        }
    }

    public class AnalyticsDashboardData
    {
        public int TotalRequests { get; set; }
        public int PendingRequests { get; set; }
        public int InProgressRequests { get; set; }
        public int CompletedRequests { get; set; }
        public int CancelledRequests { get; set; }
        public double AverageResolutionTimeHours { get; set; }

        public List<KeyValuePair<string, int>> RequestsByCategory { get; set; } = new();
        public List<KeyValuePair<string, int>> RequestsByPriority { get; set; } = new();
        public List<KeyValuePair<string, int>> RequestsByStatus { get; set; } = new();
        public List<KeyValuePair<string, int>> DailyRequestTrend { get; set; } = new();
        public List<KeyValuePair<string, int>> TopRequesters { get; set; } = new();

        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
    }
}