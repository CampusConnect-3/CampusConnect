using CampusConnect.Data;
using CampusConnect.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace CampusConnect.Services;

public class TestDataSeeder
{
    private readonly TablesDbContext _context;
    private readonly UserManager<IdentityUser> _userManager;
    private readonly ILogger<TestDataSeeder> _logger;

    public TestDataSeeder(
        TablesDbContext context,
        UserManager<IdentityUser> userManager,
        ILogger<TestDataSeeder> logger)
    {
        _context = context;
        _userManager = userManager;
        _logger = logger;
    }

    public async Task<string> SeedInsightTriggersAsync()
    {
        try
        {
            // Get or create test student
            var saraIdentity = await _userManager.FindByEmailAsync("sarabrown@school.edu");
            if (saraIdentity == null)
            {
                saraIdentity = new IdentityUser
                {
                    UserName = "sarabrown@school.edu",
                    Email = "sarabrown@school.edu",
                    EmailConfirmed = true
                };
                var result = await _userManager.CreateAsync(saraIdentity, "Student@123");
                if (!result.Succeeded)
                {
                    return $"Failed to create Sara Brown account: {string.Join(", ", result.Errors.Select(e => e.Description))}";
                }
                await _userManager.AddToRoleAsync(saraIdentity, "User");
            }

            var saraUser = await _context.users.FirstOrDefaultAsync(u => u.identityUserId == saraIdentity.Id);
            if (saraUser == null)
            {
                saraUser = new user
                {
                    identityUserId = saraIdentity.Id,
                    fName = "Sara",
                    lName = "Brown",
                    username = "sarabrown",
                    email = "sarabrown@school.edu",
                    status = "Active"
                };
                _context.users.Add(saraUser);
                await _context.SaveChangesAsync();
            }

            // Get categories and status
            var categories = await _context.category.ToListAsync();
            if (!categories.Any())
            {
                return "No categories found. Please add categories first.";
            }

            var hvacCategory = categories.FirstOrDefault(c => c.categoryName.Contains("HVAC") || c.categoryName.Contains("Air"));
            var itCategory = categories.FirstOrDefault(c => c.categoryName.Contains("IT") || c.categoryName.Contains("Technology"));
            var maintenanceCategory = categories.FirstOrDefault(c => c.categoryName.Contains("Maintenance") || c.categoryName.Contains("Repair"));
            var defaultCategory = hvacCategory ?? itCategory ?? maintenanceCategory ?? categories.First();

            var statuses = await _context.requestStatus.ToListAsync();
            var openStatus = statuses.FirstOrDefault(s => s.statusName.Contains("Open") || s.statusName.Contains("New"));
            var defaultStatus = openStatus ?? statuses.FirstOrDefault();

            var now = DateTime.Now;
            var requestsAdded = 0;

            // PATTERN 1: Science Building HVAC Issues (6 requests - triggers AI insight)
            if (hvacCategory != null)
            {
                var scienceRequests = new List<request>
                {
                    new request
                    {
                        created_by = saraUser.userID,
                        title = "Science Building - AC not cooling room 201",
                        description = "The AC unit in room 201 is running but not cooling. Temperature is currently 78°F when set to 68°F. Students are complaining about the heat during lab sessions.",
                        categoryID = hvacCategory.categoryID,
                        priority = "High",
                        statusID = defaultStatus?.statusID,
                        createdAt = now.AddDays(-25),
                        buildingName = "Science Building",
                        roomNumber = "201",
                        phoneNumber = "904-555-0101",
                        email = "sarabrown@school.edu"
                    },
                    new request
                    {
                        created_by = saraUser.userID,
                        title = "Science Building - No cold air from vents room 305",
                        description = "The HVAC system in room 305 is blowing warm air constantly. Thermostat shows cooling mode but air temperature feels like 75°F. Problem started 3 days ago.",
                        categoryID = hvacCategory.categoryID,
                        priority = "High",
                        statusID = defaultStatus?.statusID,
                        createdAt = now.AddDays(-20),
                        buildingName = "Science Building",
                        roomNumber = "305",
                        phoneNumber = "904-555-0102",
                        email = "sarabrown@school.edu"
                    },
                    new request
                    {
                        created_by = saraUser.userID,
                        title = "Science Building - Temperature fluctuations in lab 410",
                        description = "Lab 410 experiences extreme temperature swings. AC runs for 10 minutes then shuts off, causing temperature to rise. This is affecting our refrigerated specimens.",
                        categoryID = hvacCategory.categoryID,
                        priority = "Critical",
                        statusID = defaultStatus?.statusID,
                        createdAt = now.AddDays(-15),
                        buildingName = "Science Building",
                        roomNumber = "410",
                        phoneNumber = "904-555-0103",
                        email = "sarabrown@school.edu"
                    },
                    new request
                    {
                        created_by = saraUser.userID,
                        title = "Science Building - HVAC making loud noise room 215",
                        description = "Air conditioning unit making grinding noise and room temperature keeps rising. Noise started yesterday morning, cooling stopped this afternoon.",
                        categoryID = hvacCategory.categoryID,
                        priority = "High",
                        statusID = defaultStatus?.statusID,
                        createdAt = now.AddDays(-10),
                        buildingName = "Science Building",
                        roomNumber = "215",
                        phoneNumber = "904-555-0104",
                        email = "sarabrown@school.edu"
                    },
                    new request
                    {
                        created_by = saraUser.userID,
                        title = "Science Building - Classroom too hot AC not working",
                        description = "Room 102 air conditioning completely stopped working. Temperature reached 82°F during afternoon class. Students reported feeling dizzy.",
                        categoryID = hvacCategory.categoryID,
                        priority = "Critical",
                        statusID = defaultStatus?.statusID,
                        createdAt = now.AddDays(-5),
                        buildingName = "Science Building",
                        roomNumber = "102",
                        phoneNumber = "904-555-0105",
                        email = "sarabrown@school.edu"
                    },
                    new request
                    {
                        created_by = saraUser.userID,
                        title = "Science Building - Weak airflow from ceiling vents",
                        description = "The air conditioning vents in room 350 have very weak airflow. You can barely feel air coming out. Room stays warm all day despite thermostat being set to cool.",
                        categoryID = hvacCategory.categoryID,
                        priority = "Medium",
                        statusID = defaultStatus?.statusID,
                        createdAt = now.AddDays(-3),
                        buildingName = "Science Building",
                        roomNumber = "350",
                        phoneNumber = "904-555-0106",
                        email = "sarabrown@school.edu"
                    }
                };

                _context.request.AddRange(scienceRequests);
                requestsAdded += scienceRequests.Count;
            }

            // PATTERN 2: Library IT/Network Issues (5 requests - triggers AI insight)
            if (itCategory != null)
            {
                var libraryRequests = new List<request>
                {
                    new request
                    {
                        created_by = saraUser.userID,
                        title = "Library - Network connectivity issues in lab 2A",
                        description = "Computers in lab 2A keep losing network connection. Wifi drops every 15-20 minutes. Students cannot access online resources or submit assignments.",
                        categoryID = itCategory.categoryID,
                        priority = "High",
                        statusID = defaultStatus?.statusID,
                        createdAt = now.AddDays(-28),
                        buildingName = "Library",
                        roomNumber = "Lab 2A",
                        phoneNumber = "904-555-0201",
                        email = "sarabrown@school.edu"
                    },
                    new request
                    {
                        created_by = saraUser.userID,
                        title = "Library - Wifi not working on 3rd floor",
                        description = "All devices on the 3rd floor study area cannot connect to campus wifi. Error message says 'unable to obtain IP address'. Multiple students affected.",
                        categoryID = itCategory.categoryID,
                        priority = "High",
                        statusID = defaultStatus?.statusID,
                        createdAt = now.AddDays(-22),
                        buildingName = "Library",
                        roomNumber = "3rd Floor Study Area",
                        phoneNumber = "904-555-0202",
                        email = "sarabrown@school.edu"
                    },
                    new request
                    {
                        created_by = saraUser.userID,
                        title = "Library - Network extremely slow in study rooms",
                        description = "Internet connection in study rooms B1-B6 is extremely slow. Speed test shows 0.5 Mbps download when it should be 100+ Mbps. Cannot stream videos or load websites.",
                        categoryID = itCategory.categoryID,
                        priority = "Medium",
                        statusID = defaultStatus?.statusID,
                        createdAt = now.AddDays(-18),
                        buildingName = "Library",
                        roomNumber = "Study Rooms B1-B6",
                        phoneNumber = "904-555-0203",
                        email = "sarabrown@school.edu"
                    },
                    new request
                    {
                        created_by = saraUser.userID,
                        title = "Library - Public computers won't connect to internet",
                        description = "All 8 public computers on first floor show 'no internet connection' error. Network icon shows yellow exclamation mark. Rebooting doesn't fix the issue.",
                        categoryID = itCategory.categoryID,
                        priority = "High",
                        statusID = defaultStatus?.statusID,
                        createdAt = now.AddDays(-12),
                        buildingName = "Library",
                        roomNumber = "1st Floor Computer Area",
                        phoneNumber = "904-555-0204",
                        email = "sarabrown@school.edu"
                    },
                    new request
                    {
                        created_by = saraUser.userID,
                        title = "Library - Cannot access network printer",
                        description = "Network printer on 2nd floor is offline. Computers show 'printer not found' error. This printer serves the entire floor and students need to print assignments urgently.",
                        categoryID = itCategory.categoryID,
                        priority = "Medium",
                        statusID = defaultStatus?.statusID,
                        createdAt = now.AddDays(-8),
                        buildingName = "Library",
                        roomNumber = "2nd Floor Print Station",
                        phoneNumber = "904-555-0205",
                        email = "sarabrown@school.edu"
                    }
                };

                _context.request.AddRange(libraryRequests);
                requestsAdded += libraryRequests.Count;
            }

            // PATTERN 3: Student Center Plumbing Issues (4 requests - triggers AI insight)
            if (maintenanceCategory != null)
            {
                var studentCenterRequests = new List<request>
                {
                    new request
                    {
                        created_by = saraUser.userID,
                        title = "Student Center - Water leak in cafeteria ceiling",
                        description = "Large water stain on ceiling tiles above dining area section C. Water dripping during rain. Buckets placed to catch water. Ceiling tiles look damaged.",
                        categoryID = maintenanceCategory.categoryID,
                        priority = "Critical",
                        statusID = defaultStatus?.statusID,
                        createdAt = now.AddDays(-27),
                        buildingName = "Student Center",
                        roomNumber = "Cafeteria Section C",
                        phoneNumber = "904-555-0301",
                        email = "sarabrown@school.edu"
                    },
                    new request
                    {
                        created_by = saraUser.userID,
                        title = "Student Center - Bathroom pipe leaking water",
                        description = "Men's restroom on 2nd floor has water pooling near sinks. Pipe under sink #3 is leaking continuously. Floor is wet and slippery - safety hazard.",
                        categoryID = maintenanceCategory.categoryID,
                        priority = "High",
                        statusID = defaultStatus?.statusID,
                        createdAt = now.AddDays(-19),
                        buildingName = "Student Center",
                        roomNumber = "2nd Floor Men's Restroom",
                        phoneNumber = "904-555-0302",
                        email = "sarabrown@school.edu"
                    },
                    new request
                    {
                        created_by = saraUser.userID,
                        title = "Student Center - Water fountain not draining",
                        description = "Water fountain near room 215 has standing water. Drain appears clogged. Water overflows when used. Students avoiding it due to unsanitary appearance.",
                        categoryID = maintenanceCategory.categoryID,
                        priority = "Medium",
                        statusID = defaultStatus?.statusID,
                        createdAt = now.AddDays(-14),
                        buildingName = "Student Center",
                        roomNumber = "Hallway near 215",
                        phoneNumber = "904-555-0303",
                        email = "sarabrown@school.edu"
                    },
                    new request
                    {
                        created_by = saraUser.userID,
                        title = "Student Center - Leaking pipe in storage room",
                        description = "Storage room 105 has visible water damage on walls. Pipe along ceiling is dripping. Maintenance supplies getting wet. Need plumber to inspect and repair.",
                        categoryID = maintenanceCategory.categoryID,
                        priority = "High",
                        statusID = defaultStatus?.statusID,
                        createdAt = now.AddDays(-9),
                        buildingName = "Student Center",
                        roomNumber = "Storage 105",
                        phoneNumber = "904-555-0304",
                        email = "sarabrown@school.edu"
                    }
                };

                _context.request.AddRange(studentCenterRequests);
                requestsAdded += studentCenterRequests.Count;
            }

            await _context.SaveChangesAsync();

            _logger.LogInformation("Seeded {Count} test requests for AI insight patterns", requestsAdded);
            return $"✅ Successfully added {requestsAdded} pattern-based requests from Sara Brown (sarabrown@school.edu). Run 'Sync All' then 'Generate Insights' to see AI patterns!";
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error seeding test requests");
            return $"❌ Error: {ex.Message}";
        }
    }
}