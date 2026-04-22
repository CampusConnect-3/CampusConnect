using CampusConnect.Data;
using CampusConnect.Models;
using Microsoft.EntityFrameworkCore;

namespace CampusConnect.Services
{
    public class TestDataSeeder
    {
        private readonly TablesDbContext _context;
        private readonly ILogger<TestDataSeeder> _logger;

        public TestDataSeeder(TablesDbContext context, ILogger<TestDataSeeder> logger)
        {
            _context = context;
            _logger = logger;
        }

        public async Task<string> SeedKatePlumbingRequestsAsync()
        {
            try
            {
                // Get or create Kate's user account
                var kateUser = await GetOrCreateUserAsync("kateerickson57@gmail.com", "Kate", "Erickson", "User");

                // Get existing staff members (assuming they already exist)
                var staffMembers = await _context.users
                    .Where(u => u.email.Contains("@ju.edu"))
                    .Take(2)
                    .ToListAsync();

                if (staffMembers.Count < 2)
                {
                    return "❌ Error: Not enough staff members found in the system. Please ensure staff accounts exist before seeding Kate's requests.";
                }

                var staffUser1 = staffMembers[0];
                var staffUser2 = staffMembers[1];

                // Get or create categories and statuses
                var plumbingCategory = await GetOrCreateCategoryAsync("Plumbing");
                
                var todoStatus = await GetOrCreateStatusAsync("To-Do");
                var inProgressStatus = await GetOrCreateStatusAsync("In Progress");
                var completedStatus = await GetOrCreateStatusAsync("Completed");

                var baseDate = DateTime.Now.AddDays(-45);

                var plumbingRequests = new List<request>
                {
                    // Completed plumbing requests
                    new request
                    {
                        created_by = kateUser.userID,
                        assigned_to = staffUser1.userID,
                        title = "Leaking sink in Kinne Hall 304",
                        description = "The bathroom sink has been dripping constantly for 3 days. Water is pooling on the floor and creating a slip hazard.",
                        categoryID = plumbingCategory.categoryID,
                        priority = "High",
                        statusID = completedStatus.statusID,
                        createdAt = baseDate.AddDays(5).AddHours(8),
                        closedAt = baseDate.AddDays(5).AddHours(14),
                        buildingName = "Kinne Hall",
                        roomNumber = "304",
                        phoneNumber = "904-555-0157",
                        email = kateUser.email
                    },
                    new request
                    {
                        created_by = kateUser.userID,
                        assigned_to = staffUser2.userID,
                        title = "Clogged toilet in Phillips Hall women's restroom",
                        description = "Toilet on first floor is completely clogged and overflowing. Unable to use that restroom.",
                        categoryID = plumbingCategory.categoryID,
                        priority = "High",
                        statusID = completedStatus.statusID,
                        createdAt = baseDate.AddDays(12).AddHours(10),
                        closedAt = baseDate.AddDays(12).AddHours(11.5),
                        buildingName = "Phillips Hall",
                        roomNumber = "1st Floor Women's Restroom",
                        phoneNumber = "904-555-0157",
                        email = kateUser.email
                    },
                    new request
                    {
                        created_by = kateUser.userID,
                        assigned_to = staffUser1.userID,
                        title = "Low water pressure in Davis Hall water fountain",
                        description = "Water fountain barely trickles water. Students can't fill water bottles.",
                        categoryID = plumbingCategory.categoryID,
                        priority = "Medium",
                        statusID = completedStatus.statusID,
                        createdAt = baseDate.AddDays(18).AddHours(9),
                        closedAt = baseDate.AddDays(19).AddHours(15),
                        buildingName = "Davis Hall",
                        roomNumber = "2nd Floor Hallway",
                        phoneNumber = "904-555-0157",
                        email = kateUser.email
                    },
                    new request
                    {
                        created_by = kateUser.userID,
                        assigned_to = staffUser2.userID,
                        title = "Hot water not working in Science Building lab sink",
                        description = "Lab sink only has cold water. Need hot water for experiments and safety protocols.",
                        categoryID = plumbingCategory.categoryID,
                        priority = "High",
                        statusID = completedStatus.statusID,
                        createdAt = baseDate.AddDays(25).AddHours(11),
                        closedAt = baseDate.AddDays(26).AddHours(9),
                        buildingName = "Science Building",
                        roomNumber = "Lab 210",
                        phoneNumber = "904-555-0157",
                        email = kateUser.email
                    },
                    new request
                    {
                        created_by = kateUser.userID,
                        assigned_to = staffUser1.userID,
                        title = "Dripping shower in Athletic Center locker room",
                        description = "Shower head continuously drips even when turned off. Wasting water.",
                        categoryID = plumbingCategory.categoryID,
                        priority = "Low",
                        statusID = completedStatus.statusID,
                        createdAt = baseDate.AddDays(30).AddHours(7),
                        closedAt = baseDate.AddDays(32).AddHours(13),
                        buildingName = "Athletic Center",
                        roomNumber = "Women's Locker Room",
                        phoneNumber = "904-555-0157",
                        email = kateUser.email
                    },

                    // In Progress requests
                    new request
                    {
                        created_by = kateUser.userID,
                        assigned_to = staffUser2.userID,
                        title = "Broken pipe in Library basement",
                        description = "Loud banging noise from pipes in ceiling. Sounds like water hammer or loose pipe.",
                        categoryID = plumbingCategory.categoryID,
                        priority = "Medium",
                        statusID = inProgressStatus.statusID,
                        createdAt = baseDate.AddDays(38).AddHours(14),
                        closedAt = null,
                        buildingName = "Library",
                        roomNumber = "Basement Storage",
                        phoneNumber = "904-555-0157",
                        email = kateUser.email
                    },
                    new request
                    {
                        created_by = kateUser.userID,
                        assigned_to = staffUser1.userID,
                        title = "Slow draining sink in Student Center kitchen",
                        description = "Kitchen sink drains very slowly. Standing water accumulates after washing dishes.",
                        categoryID = plumbingCategory.categoryID,
                        priority = "Medium",
                        statusID = inProgressStatus.statusID,
                        createdAt = baseDate.AddDays(40).AddHours(16),
                        closedAt = null,
                        buildingName = "Student Center",
                        roomNumber = "Kitchen",
                        phoneNumber = "904-555-0157",
                        email = kateUser.email
                    },

                    // Recent To-Do requests
                    new request
                    {
                        created_by = kateUser.userID,
                        assigned_to = staffUser2.userID,
                        title = "Running toilet in Administration Building 2nd floor",
                        description = "Toilet runs continuously, wasting water and making noise. Handle doesn't stop the flow.",
                        categoryID = plumbingCategory.categoryID,
                        priority = "Medium",
                        statusID = todoStatus.statusID,
                        createdAt = DateTime.Now.AddHours(-48),
                        closedAt = null,
                        buildingName = "Administration Building",
                        roomNumber = "2nd Floor Men's Restroom",
                        phoneNumber = "904-555-0157",
                        email = kateUser.email
                    },
                    new request
                    {
                        created_by = kateUser.userID,
                        assigned_to = staffUser1.userID,
                        title = "Water fountain not working in Engineering Building",
                        description = "Water fountain button is pressed but no water comes out. Might be a valve issue.",
                        categoryID = plumbingCategory.categoryID,
                        priority = "Low",
                        statusID = todoStatus.statusID,
                        createdAt = DateTime.Now.AddHours(-24),
                        closedAt = null,
                        buildingName = "Engineering Building",
                        roomNumber = "3rd Floor",
                        phoneNumber = "904-555-0157",
                        email = kateUser.email
                    },
                    new request
                    {
                        created_by = kateUser.userID,
                        assigned_to = staffUser2.userID,
                        title = "Foul odor from drain in Residence Hall A bathroom",
                        description = "Strong sewer smell coming from bathroom drain. Residents are complaining.",
                        categoryID = plumbingCategory.categoryID,
                        priority = "High",
                        statusID = todoStatus.statusID,
                        createdAt = DateTime.Now.AddHours(-6),
                        closedAt = null,
                        buildingName = "Residence Hall A",
                        roomNumber = "Room 215 Bathroom",
                        phoneNumber = "904-555-0157",
                        email = kateUser.email
                    }
                };

                _context.request.AddRange(plumbingRequests);
                await _context.SaveChangesAsync();

                _logger.LogInformation("Successfully seeded {Count} plumbing requests for Kate Erickson", plumbingRequests.Count);
                return $"✅ Successfully seeded {plumbingRequests.Count} plumbing requests for Kate Erickson (kateerickson57@gmail.com)!\n\n" +
                       $"Breakdown:\n" +
                       $"• 5 Completed requests\n" +
                       $"• 2 In Progress requests\n" +
                       $"• 3 To-Do requests\n\n" +
                       $"Assigned to existing staff: {staffUser1.fName} {staffUser1.lName} and {staffUser2.fName} {staffUser2.lName}\n\n" +
                       $"Request types include: leaking sinks, clogged toilets, water pressure issues, hot water problems, and drain issues.";
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to seed Kate's plumbing requests");
                return $"❌ Failed to seed Kate's plumbing requests: {ex.Message}";
            }
        }

        public async Task<string> SeedInsightTriggersAsync()
        {
            try
            {
                // Get or create required data
                var requester = await GetOrCreateUserAsync("sarabrown@school.edu", "Sara", "Brown", "User");
                var staffUser1 = await GetOrCreateUserAsync("john.tech@ju.edu", "John", "Technician", "Staff");
                var staffUser2 = await GetOrCreateUserAsync("sarah.support@ju.edu", "Sarah", "Support", "Staff");

                var itCategory = await GetOrCreateCategoryAsync("IT Support");
                var facilitiesCategory = await GetOrCreateCategoryAsync("Facilities");
                var academicCategory = await GetOrCreateCategoryAsync("Academic Services");

                var pendingStatus = await GetOrCreateStatusAsync("Pending");
                var inProgressStatus = await GetOrCreateStatusAsync("In Progress");
                var completedStatus = await GetOrCreateStatusAsync("Completed");

                var baseDate = DateTime.Now.AddDays(-30);
                var requests = new List<request>
                {
                    // Pattern 1: Top Performer - Quick resolutions for high-priority IT issues
                    new request
                    {
                        created_by = requester.userID,
                        assigned_to = staffUser1.userID,
                        title = "Projector not working in Davis Hall 201",
                        description = "The projector won't turn on. Need urgent help for my lecture.",
                        categoryID = itCategory.categoryID,
                        priority = "High",
                        statusID = completedStatus.statusID,
                        createdAt = baseDate.AddDays(1).AddHours(9),
                        closedAt = baseDate.AddDays(1).AddHours(10.5), // 1.5 hours
                        buildingName = "Davis Hall",
                        roomNumber = "201",
                        phoneNumber = "904-256-1234",
                        email = requester.email
                    },
                    new request
                    {
                        created_by = requester.userID,
                        assigned_to = staffUser1.userID,
                        title = "Computer won't boot in Library Lab",
                        description = "Computer stuck on loading screen.",
                        categoryID = itCategory.categoryID,
                        priority = "Medium",
                        statusID = completedStatus.statusID,
                        createdAt = baseDate.AddDays(3).AddHours(10),
                        closedAt = baseDate.AddDays(3).AddHours(12), // 2 hours
                        buildingName = "Library",
                        roomNumber = "Lab A",
                        phoneNumber = "904-256-1234",
                        email = requester.email
                    },
                    new request
                    {
                        created_by = requester.userID,
                        assigned_to = staffUser1.userID,
                        title = "Network connectivity issues in Science Building",
                        description = "Wi-Fi keeps disconnecting during class.",
                        categoryID = itCategory.categoryID,
                        priority = "High",
                        statusID = completedStatus.statusID,
                        createdAt = baseDate.AddDays(5).AddHours(14),
                        closedAt = baseDate.AddDays(5).AddHours(15.5), // 1.5 hours
                        buildingName = "Science Building",
                        roomNumber = "305",
                        phoneNumber = "904-256-1234",
                        email = requester.email
                    },
                    new request
                    {
                        created_by = requester.userID,
                        assigned_to = staffUser1.userID,
                        title = "Smartboard calibration needed",
                        description = "Touch screen not aligned properly.",
                        categoryID = itCategory.categoryID,
                        priority = "Medium",
                        statusID = completedStatus.statusID,
                        createdAt = baseDate.AddDays(10).AddHours(9),
                        closedAt = baseDate.AddDays(10).AddHours(14), // 5 hours
                        buildingName = "Engineering Building",
                        roomNumber = "Lab 5",
                        phoneNumber = "904-256-1234",
                        email = requester.email
                    },
                    new request
                    {
                        created_by = requester.userID,
                        assigned_to = staffUser1.userID,
                        title = "Software installation - AutoCAD",
                        description = "Need AutoCAD installed for engineering course.",
                        categoryID = itCategory.categoryID,
                        priority = "Medium",
                        statusID = completedStatus.statusID,
                        createdAt = baseDate.AddDays(12).AddHours(10),
                        closedAt = baseDate.AddDays(12).AddHours(16), // 6 hours
                        buildingName = "Engineering Building",
                        roomNumber = "Computer Lab 3",
                        phoneNumber = "904-256-1234",
                        email = requester.email
                    },
                    new request
                    {
                        created_by = requester.userID,
                        assigned_to = staffUser1.userID,
                        title = "Password reset for student account",
                        description = "Student locked out of campus portal.",
                        categoryID = itCategory.categoryID,
                        priority = "High",
                        statusID = completedStatus.statusID,
                        createdAt = baseDate.AddDays(15).AddHours(8),
                        closedAt = baseDate.AddDays(15).AddHours(8.5), // 30 minutes
                        buildingName = "IT Help Desk",
                        roomNumber = "Main",
                        phoneNumber = "904-256-1234",
                        email = requester.email
                    },
                    new request
                    {
                        created_by = requester.userID,
                        assigned_to = staffUser1.userID,
                        title = "Classroom reservation system error",
                        description = "Can't book rooms through the online system.",
                        categoryID = academicCategory.categoryID,
                        priority = "Medium",
                        statusID = completedStatus.statusID,
                        createdAt = baseDate.AddDays(18).AddHours(9),
                        closedAt = baseDate.AddDays(18).AddHours(13), // 4 hours
                        buildingName = "Academic Affairs",
                        roomNumber = "Office",
                        phoneNumber = "904-256-1234",
                        email = requester.email
                    },
                    new request
                    {
                        created_by = requester.userID,
                        assigned_to = staffUser1.userID,
                        title = "Video conferencing setup for remote class",
                        description = "Need Zoom room configured for hybrid teaching.",
                        categoryID = itCategory.categoryID,
                        priority = "High",
                        statusID = completedStatus.statusID,
                        createdAt = baseDate.AddDays(20).AddHours(10),
                        closedAt = baseDate.AddDays(20).AddHours(12.5), // 2.5 hours
                        buildingName = "Davis Hall",
                        roomNumber = "Conference Room A",
                        phoneNumber = "904-256-1234",
                        email = requester.email
                    },

                    // Pattern 2: Staff member with slower resolution times
                    new request
                    {
                        created_by = requester.userID,
                        assigned_to = staffUser2.userID,
                        title = "Printer jam in Administration Office",
                        description = "Paper is stuck and won't print.",
                        categoryID = facilitiesCategory.categoryID,
                        priority = "Low",
                        statusID = completedStatus.statusID,
                        createdAt = baseDate.AddDays(2).AddHours(9),
                        closedAt = baseDate.AddDays(5).AddHours(16), // 79 hours
                        buildingName = "Administration Building",
                        roomNumber = "102",
                        phoneNumber = "904-256-5678",
                        email = requester.email
                    },
                    new request
                    {
                        created_by = requester.userID,
                        assigned_to = staffUser2.userID,
                        title = "AC not working in Student Center",
                        description = "Room is very hot, AC unit not responding.",
                        categoryID = facilitiesCategory.categoryID,
                        priority = "High",
                        statusID = completedStatus.statusID,
                        createdAt = baseDate.AddDays(7).AddHours(11),
                        closedAt = baseDate.AddDays(10).AddHours(15), // 76 hours
                        buildingName = "Student Center",
                        roomNumber = "Main Hall",
                        phoneNumber = "904-256-5678",
                        email = requester.email
                    },
                    new request
                    {
                        created_by = requester.userID,
                        assigned_to = staffUser2.userID,
                        title = "Broken vending machine",
                        description = "Vending machine took money but didn't dispense item.",
                        categoryID = facilitiesCategory.categoryID,
                        priority = "Low",
                        statusID = inProgressStatus.statusID,
                        createdAt = baseDate.AddDays(8),
                        closedAt = null,
                        buildingName = "Student Center",
                        roomNumber = "1st Floor",
                        phoneNumber = "904-256-5678",
                        email = requester.email
                    },

                    // Pattern 3: High-priority pending requests showing urgency
                    new request
                    {
                        created_by = requester.userID,
                        assigned_to = staffUser1.userID,
                        title = "Security system malfunction in Parking Garage",
                        description = "Gate won't open, students can't exit.",
                        categoryID = facilitiesCategory.categoryID,
                        priority = "High",
                        statusID = pendingStatus.statusID,
                        createdAt = DateTime.Now.AddHours(-2),
                        closedAt = null,
                        buildingName = "Parking Garage B",
                        roomNumber = "Exit 1",
                        phoneNumber = "904-256-9999",
                        email = requester.email
                    },
                    new request
                    {
                        created_by = requester.userID,
                        assigned_to = staffUser1.userID,
                        title = "Fire alarm system test needed",
                        description = "Annual fire alarm testing required by code.",
                        categoryID = facilitiesCategory.categoryID,
                        priority = "High",
                        statusID = pendingStatus.statusID,
                        createdAt = DateTime.Now.AddHours(-48),
                        closedAt = null,
                        buildingName = "Residence Hall A",
                        roomNumber = "All Floors",
                        phoneNumber = "904-256-7777",
                        email = requester.email
                    },

                    // Pattern 4: Old pending requests showing backlog
                    new request
                    {
                        created_by = requester.userID,
                        assigned_to = staffUser2.userID,
                        title = "Landscaping complaint - overgrown bushes",
                        description = "Bushes blocking walkway near entrance.",
                        categoryID = facilitiesCategory.categoryID,
                        priority = "Low",
                        statusID = pendingStatus.statusID,
                        createdAt = baseDate.AddDays(1),
                        closedAt = null,
                        buildingName = "Main Campus",
                        roomNumber = "Exterior",
                        phoneNumber = "904-256-5678",
                        email = requester.email
                    },

                    // Pattern 5: HVAC issues in Science Building (triggers building pattern insights)
                    new request
                    {
                        created_by = requester.userID,
                        assigned_to = staffUser2.userID,
                        title = "Science Building Room 101 - HVAC Failure",
                        description = "Room temperature is 85°F, students complaining.",
                        categoryID = facilitiesCategory.categoryID,
                        priority = "High",
                        statusID = completedStatus.statusID,
                        createdAt = baseDate.AddDays(4),
                        closedAt = baseDate.AddDays(5),
                        buildingName = "Science Building",
                        roomNumber = "101",
                        phoneNumber = "904-256-3333",
                        email = requester.email
                    },
                    new request
                    {
                        created_by = requester.userID,
                        assigned_to = staffUser2.userID,
                        title = "Science Building Room 205 - AC Not Cooling",
                        description = "AC running but not cooling the lab.",
                        categoryID = facilitiesCategory.categoryID,
                        priority = "Medium",
                        statusID = completedStatus.statusID,
                        createdAt = baseDate.AddDays(8),
                        closedAt = baseDate.AddDays(9),
                        buildingName = "Science Building",
                        roomNumber = "205",
                        phoneNumber = "904-256-3333",
                        email = requester.email
                    },
                    new request
                    {
                        created_by = requester.userID,
                        assigned_to = staffUser1.userID,
                        title = "Science Building Room 310 - Thermostat Broken",
                        description = "Cannot adjust temperature, stuck at 60°F.",
                        categoryID = facilitiesCategory.categoryID,
                        priority = "Medium",
                        statusID = inProgressStatus.statusID,
                        createdAt = baseDate.AddDays(14),
                        closedAt = null,
                        buildingName = "Science Building",
                        roomNumber = "310",
                        phoneNumber = "904-256-3333",
                        email = requester.email
                    },
                    new request
                    {
                        created_by = requester.userID,
                        assigned_to = staffUser2.userID,
                        title = "Science Building Hallway - Poor Ventilation",
                        description = "3rd floor hallway has no air circulation.",
                        categoryID = facilitiesCategory.categoryID,
                        priority = "Low",
                        statusID = pendingStatus.statusID,
                        createdAt = baseDate.AddDays(18),
                        closedAt = null,
                        buildingName = "Science Building",
                        roomNumber = "3rd Floor Hallway",
                        phoneNumber = "904-256-3333",
                        email = requester.email
                    },
                    new request
                    {
                        created_by = requester.userID,
                        assigned_to = staffUser1.userID,
                        title = "Science Building Lab 401 - HVAC Making Loud Noise",
                        description = "Loud banging noise from ceiling HVAC unit.",
                        categoryID = facilitiesCategory.categoryID,
                        priority = "High",
                        statusID = completedStatus.statusID,
                        createdAt = baseDate.AddDays(22),
                        closedAt = baseDate.AddDays(23),
                        buildingName = "Science Building",
                        roomNumber = "Lab 401",
                        phoneNumber = "904-256-3333",
                        email = requester.email
                    },
                    new request
                    {
                        created_by = requester.userID,
                        assigned_to = staffUser2.userID,
                        title = "Science Building - Central Air System Issue",
                        description = "Multiple rooms reporting temperature problems.",
                        categoryID = facilitiesCategory.categoryID,
                        priority = "High",
                        statusID = inProgressStatus.statusID,
                        createdAt = DateTime.Now.AddDays(-3),
                        closedAt = null,
                        buildingName = "Science Building",
                        roomNumber = "Mechanical Room",
                        phoneNumber = "904-256-3333",
                        email = requester.email
                    },

                    // Pattern 6: Network issues in Library (triggers building pattern insights)
                    new request
                    {
                        created_by = requester.userID,
                        assigned_to = staffUser1.userID,
                        title = "Library Study Room A - No Wi-Fi",
                        description = "Cannot connect to Wi-Fi network.",
                        categoryID = itCategory.categoryID,
                        priority = "Medium",
                        statusID = completedStatus.statusID,
                        createdAt = baseDate.AddDays(6),
                        closedAt = baseDate.AddDays(6).AddHours(3),
                        buildingName = "Library",
                        roomNumber = "Study Room A",
                        phoneNumber = "904-256-4444",
                        email = requester.email
                    },
                    new request
                    {
                        created_by = requester.userID,
                        assigned_to = staffUser1.userID,
                        title = "Library 2nd Floor - Slow Internet",
                        description = "Internet extremely slow, cannot load pages.",
                        categoryID = itCategory.categoryID,
                        priority = "High",
                        statusID = completedStatus.statusID,
                        createdAt = baseDate.AddDays(11),
                        closedAt = baseDate.AddDays(11).AddHours(2),
                        buildingName = "Library",
                        roomNumber = "2nd Floor",
                        phoneNumber = "904-256-4444",
                        email = requester.email
                    },
                    new request
                    {
                        created_by = requester.userID,
                        assigned_to = staffUser1.userID,
                        title = "Library Computer Lab - Network Drops",
                        description = "Computers keep losing network connection.",
                        categoryID = itCategory.categoryID,
                        priority = "High",
                        statusID = completedStatus.statusID,
                        createdAt = baseDate.AddDays(16),
                        closedAt = baseDate.AddDays(16).AddHours(4),
                        buildingName = "Library",
                        roomNumber = "Computer Lab",
                        phoneNumber = "904-256-4444",
                        email = requester.email
                    },
                    new request
                    {
                        created_by = requester.userID,
                        assigned_to = staffUser1.userID,
                        title = "Library Main Floor - Intermittent Connectivity",
                        description = "Wi-Fi connects and disconnects randomly.",
                        categoryID = itCategory.categoryID,
                        priority = "Medium",
                        statusID = inProgressStatus.statusID,
                        createdAt = baseDate.AddDays(21),
                        closedAt = null,
                        buildingName = "Library",
                        roomNumber = "Main Floor",
                        phoneNumber = "904-256-4444",
                        email = requester.email
                    },
                    new request
                    {
                        created_by = requester.userID,
                        assigned_to = staffUser1.userID,
                        title = "Library Group Study Area - Access Point Down",
                        description = "Wi-Fi access point appears to be offline.",
                        categoryID = itCategory.categoryID,
                        priority = "High",
                        statusID = pendingStatus.statusID,
                        createdAt = DateTime.Now.AddDays(-2),
                        closedAt = null,
                        buildingName = "Library",
                        roomNumber = "Group Study Area",
                        phoneNumber = "904-256-4444",
                        email = requester.email
                    },

                    // Pattern 7: Plumbing issues in Student Center (triggers building pattern insights)
                    new request
                    {
                        created_by = requester.userID,
                        assigned_to = staffUser2.userID,
                        title = "Student Center Bathroom - Leaking Faucet",
                        description = "Men's bathroom faucet won't stop dripping.",
                        categoryID = facilitiesCategory.categoryID,
                        priority = "Low",
                        statusID = completedStatus.statusID,
                        createdAt = baseDate.AddDays(9),
                        closedAt = baseDate.AddDays(12),
                        buildingName = "Student Center",
                        roomNumber = "Men's Bathroom 1st Floor",
                        phoneNumber = "904-256-5555",
                        email = requester.email
                    },
                    new request
                    {
                        created_by = requester.userID,
                        assigned_to = staffUser2.userID,
                        title = "Student Center Kitchen - Clogged Drain",
                        description = "Kitchen sink won't drain, standing water.",
                        categoryID = facilitiesCategory.categoryID,
                        priority = "High",
                        statusID = completedStatus.statusID,
                        createdAt = baseDate.AddDays(13),
                        closedAt = baseDate.AddDays(14),
                        buildingName = "Student Center",
                        roomNumber = "Kitchen",
                        phoneNumber = "904-256-5555",
                        email = requester.email
                    },
                    new request
                    {
                        created_by = requester.userID,
                        assigned_to = staffUser2.userID,
                        title = "Student Center Women's Bathroom - Toilet Overflow",
                        description = "Toilet overflowing, needs immediate attention.",
                        categoryID = facilitiesCategory.categoryID,
                        priority = "High",
                        statusID = completedStatus.statusID,
                        createdAt = baseDate.AddDays(19),
                        closedAt = baseDate.AddDays(19).AddHours(1),
                        buildingName = "Student Center",
                        roomNumber = "Women's Bathroom 2nd Floor",
                        phoneNumber = "904-256-5555",
                        email = requester.email
                    },
                    new request
                    {
                        created_by = requester.userID,
                        assigned_to = staffUser2.userID,
                        title = "Student Center Lounge - Water Fountain Not Working",
                        description = "Water fountain button stuck, no water flow.",
                        categoryID = facilitiesCategory.categoryID,
                        priority = "Low",
                        statusID = inProgressStatus.statusID,
                        createdAt = baseDate.AddDays(24),
                        closedAt = null,
                        buildingName = "Student Center",
                        roomNumber = "Lounge",
                        phoneNumber = "904-256-5555",
                        email = requester.email
                    }
                };

                _context.request.AddRange(requests);
                await _context.SaveChangesAsync();

                _logger.LogInformation("Successfully seeded {Count} sample requests for AI insights", requests.Count);
                return $"✅ Successfully seeded {requests.Count} sample requests! Patterns created:\n" +
                       $"• 6 HVAC issues in Science Building (triggers building insight)\n" +
                       $"• 5 Network issues in Library (triggers building insight)\n" +
                       $"• 4 Plumbing issues in Student Center (triggers building insight)\n" +
                       $"• Staff performance patterns (Top Performer vs Room for Growth)\n" +
                       $"• High-priority pending requests showing urgency\n\n" +
                       $"👉 Next: Click 'Sync All Requests' then 'Generate AI Insights'";
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to seed test data");
                return $"❌ Failed to seed test data: {ex.Message}";
            }
        }

        private async Task<user> GetOrCreateUserAsync(string email, string firstName, string lastName, string roleName)
        {
            var existingUser = await _context.users.FirstOrDefaultAsync(u => u.email == email);
            if (existingUser != null)
            {
                return existingUser;
            }

            var newUser = new user
            {
                fName = firstName,
                lName = lastName,
                username = email.Split('@')[0],
                email = email,
                status = "Active",
                password = null
            };

            _context.users.Add(newUser);
            await _context.SaveChangesAsync();

            return newUser;
        }

        private async Task<category> GetOrCreateCategoryAsync(string categoryName)
        {
            var existingCategory = await _context.category.FirstOrDefaultAsync(c => c.categoryName == categoryName);
            if (existingCategory != null)
            {
                return existingCategory;
            }

            var newCategory = new category
            {
                categoryName = categoryName
            };

            _context.category.Add(newCategory);
            await _context.SaveChangesAsync();

            return newCategory;
        }

        private async Task<requestStatus> GetOrCreateStatusAsync(string statusName)
        {
            var existingStatus = await _context.requestStatus.FirstOrDefaultAsync(s => s.statusName == statusName);
            if (existingStatus != null)
            {
                return existingStatus;
            }

            var newStatus = new requestStatus
            {
                statusName = statusName
            };

            _context.requestStatus.Add(newStatus);
            await _context.SaveChangesAsync();

            return newStatus;
        }
    }
}