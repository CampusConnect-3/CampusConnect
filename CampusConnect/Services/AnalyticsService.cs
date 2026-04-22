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

        // NEW: Staff/Technician-specific analytics
        public async Task<StaffAnalyticsData> GetStaffDashboardDataAsync(int staffUserId, DateTime? startDate = null, DateTime? endDate = null)
        {
            // Default to last 30 days if no dates provided
            startDate ??= DateTime.Now.AddDays(-30);
            endDate ??= DateTime.Now;

            // Get all requests assigned to this staff member
            var assignedRequests = await _context.request
                .Include(r => r.category)
                .Include(r => r.status)
                .Include(r => r.createdBy)
                .Where(r => r.assigned_to == staffUserId && r.createdAt >= startDate && r.createdAt <= endDate)
                .ToListAsync();

            // Get currently active (non-closed) requests
            var activeRequests = await _context.request
                .Include(r => r.category)
                .Include(r => r.status)
                .Include(r => r.createdBy)
                .Where(r => r.assigned_to == staffUserId && 
                           !r.closedAt.HasValue &&
                           (r.status.statusName == "Pending" || r.status.statusName == "In Progress"))
                .OrderBy(r => r.createdAt)
                .Take(5)
                .ToListAsync();

            // CHANGED: Include BOTH "Completed" (awaiting review) and "Closed" (archived) for analytics
            var completedRequests = assignedRequests
                .Where(r => r.status?.statusName == "Completed" || r.status?.statusName == "Closed")
                .ToList();
            
            var inProgressCount = assignedRequests.Count(r => r.status?.statusName == "In Progress");
            var pendingCount = assignedRequests.Count(r => r.status?.statusName == "Pending");

            // Get team averages for comparison (all staff members)
            var teamComparison = await CalculateTeamAveragesAsync(startDate.Value, endDate.Value);

            return new StaffAnalyticsData
            {
                TotalAssignedRequests = assignedRequests.Count,
                CompletedRequests = completedRequests.Count,
                InProgressRequests = inProgressCount,
                PendingRequests = pendingCount,
                
                CompletionRate = assignedRequests.Count > 0 
                    ? Math.Round((double)completedRequests.Count / assignedRequests.Count * 100, 1) 
                    : 0,

                AverageResolutionTimeHours = CalculateAverageResolutionTime(completedRequests),

                RequestsByCategory = assignedRequests
                    .GroupBy(r => r.category?.categoryName ?? "Uncategorized")
                    .Select(g => new KeyValuePair<string, int>(g.Key, g.Count()))
                    .OrderByDescending(kv => kv.Value)
                    .ToList(),

                RequestsByPriority = assignedRequests
                    .GroupBy(r => r.priority)
                    .Select(g => new KeyValuePair<string, int>(g.Key, g.Count()))
                    .OrderByDescending(kv => kv.Value)
                    .ToList(),

                DailyCompletionTrend = GetDailyCompletionTrend(completedRequests, startDate.Value, endDate.Value),

                RecentActiveRequests = activeRequests.Select(r => new StaffRequestSummary
                {
                    RequestID = r.requestID,
                    Title = r.title,
                    Category = r.category?.categoryName ?? "Uncategorized",
                    Priority = r.priority,
                    Status = r.status?.statusName ?? "Unknown",
                    CreatedAt = r.createdAt,
                    DaysOpen = (DateTime.Now - r.createdAt).Days
                }).ToList(),

                // Team comparison data
                TeamComparison = teamComparison,

                StartDate = startDate.Value,
                EndDate = endDate.Value
            };
        }

        // NEW: Calculate team-wide averages for comparison
        private async Task<TeamComparisonData> CalculateTeamAveragesAsync(DateTime startDate, DateTime endDate)
        {
            // Get all staff/technician assignments in the date range
            var allStaffRequests = await _context.request
                .Include(r => r.status)
                .Include(r => r.assignedTo)
                .ThenInclude(u => u.userRoles)
                .ThenInclude(ur => ur.role)
                .Where(r => r.assigned_to.HasValue && 
                           r.createdAt >= startDate && 
                           r.createdAt <= endDate)
                .ToListAsync();

            // Filter to only staff role (not managers/admins)
            var staffRequests = allStaffRequests
                .Where(r => r.assignedTo != null && 
                           r.assignedTo.userRoles.Any(ur => ur.role.roleName == "Staff"))
                .ToList();

            if (!staffRequests.Any())
            {
                return new TeamComparisonData();
            }

            // Group by staff member
            var staffGroups = staffRequests
                .GroupBy(r => r.assigned_to)
                .ToList();

            var staffCount = staffGroups.Count;

            // Calculate averages
            var totalAssignedAvg = Math.Round((double)staffRequests.Count / staffCount, 1);
            
            // CHANGED: Count both "Completed" and "Closed" as finished requests
            var completedRequests = staffRequests
                .Where(r => r.status?.statusName == "Completed" || r.status?.statusName == "Closed")
                .ToList();
            var completedAvg = Math.Round((double)completedRequests.Count / staffCount, 1);
            
            var avgCompletionRate = staffGroups.Average(g =>
            {
                var total = g.Count();
                // CHANGED: Count both "Completed" and "Closed"
                var completed = g.Count(r => r.status?.statusName == "Completed" || r.status?.statusName == "Closed");
                return total > 0 ? (double)completed / total * 100 : 0;
            });

            var avgResolutionTime = CalculateAverageResolutionTime(completedRequests);

            return new TeamComparisonData
            {
                AverageTotalAssigned = totalAssignedAvg,
                AverageCompleted = completedAvg,
                AverageCompletionRate = Math.Round(avgCompletionRate, 1),
                AverageResolutionTimeHours = avgResolutionTime,
                TeamMemberCount = staffCount
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

        private List<KeyValuePair<string, int>> GetDailyCompletionTrend(List<request> completedRequests, DateTime startDate, DateTime endDate)
        {
            var dailyData = new List<KeyValuePair<string, int>>();

            for (var date = startDate.Date; date <= endDate.Date; date = date.AddDays(1))
            {
                var count = completedRequests.Count(r => r.closedAt.HasValue && r.closedAt.Value.Date == date);
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

    public class StaffAnalyticsData
    {
        public int TotalAssignedRequests { get; set; }
        public int CompletedRequests { get; set; }
        public int InProgressRequests { get; set; }
        public int PendingRequests { get; set; }
        public double CompletionRate { get; set; }
        public double AverageResolutionTimeHours { get; set; }

        public List<KeyValuePair<string, int>> RequestsByCategory { get; set; } = new();
        public List<KeyValuePair<string, int>> RequestsByPriority { get; set; } = new();
        public List<KeyValuePair<string, int>> DailyCompletionTrend { get; set; } = new();
        public List<StaffRequestSummary> RecentActiveRequests { get; set; } = new();
        
        // NEW: Team comparison data
        public TeamComparisonData TeamComparison { get; set; } = new();

        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
    }

    public class TeamComparisonData
    {
        public double AverageTotalAssigned { get; set; }
        public double AverageCompleted { get; set; }
        public double AverageCompletionRate { get; set; }
        public double AverageResolutionTimeHours { get; set; }
        public int TeamMemberCount { get; set; }
    }

    public class StaffRequestSummary
    {
        public int RequestID { get; set; }
        public string Title { get; set; } = string.Empty;
        public string Category { get; set; } = string.Empty;
        public string Priority { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }
        public int DaysOpen { get; set; }
    }
}