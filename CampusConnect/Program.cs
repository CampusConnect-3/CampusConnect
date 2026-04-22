using CampusConnect.Configuration;
using CampusConnect.Data;
using CampusConnect.Middleware;
using CampusConnect.Services;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddRazorPages();
builder.Services.AddScoped<INotificationService, NotificationService>();
builder.Services.AddScoped<IActivityLoggerService, ActivityLoggerService>();
builder.Services.AddScoped<AnalyticsService>();
builder.Services.AddHttpContextAccessor(); // Required for getting HTTP context

if (builder.Environment.IsDevelopment())
{
    // Make auth cookies invalid after each app restart during local development.
    builder.Services.AddDataProtection().UseEphemeralDataProtectionProvider();
}

// DbContext Registration
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlServer(
        builder.Configuration.GetConnectionString("ApplicationDbContext")
        ?? throw new InvalidOperationException("Connection string 'ApplicationDbContext' not found.")
    )
);

// Register the TablesDbContext for legacy tables
builder.Services.AddDbContext<TablesDbContext>(options =>
    options.UseSqlServer(
        builder.Configuration.GetConnectionString("TablesDbContext")
        ?? throw new InvalidOperationException("Connection string 'TablesDbContext' not found.")
    )
);

// Register the Identity services - STICK WITH IdentityUser
builder.Services.AddIdentity<IdentityUser, IdentityRole>(options =>
{
    options.SignIn.RequireConfirmedAccount = false;
})
.AddEntityFrameworkStores<ApplicationDbContext>()
.AddDefaultUI()
.AddDefaultTokenProviders();

// Secure session management for Identity = harden the auth cookie
builder.Services.ConfigureApplicationCookie(options =>
{
    options.Cookie.HttpOnly = true;
    options.Cookie.SecurePolicy = CookieSecurePolicy.Always; // HTTPS-only
    options.Cookie.SameSite = SameSiteMode.Lax;              // Strong CSRF protection; avoids breaking common flows

    options.ExpireTimeSpan = TimeSpan.FromMinutes(30);       // session lifetime
    options.SlidingExpiration = true;                        // refresh on activity

    options.LoginPath = "/Identity/Account/Login";
    options.LogoutPath = "/Identity/Account/Logout";
    options.AccessDeniedPath = "/Identity/Account/AccessDenied";
});

// Configure MongoDB
builder.Services.Configure<MongoDBSettings>(
    builder.Configuration.GetSection("MongoDBSettings"));

builder.Services.AddSingleton<MongoDBService>();
builder.Services.AddScoped<RequestSyncService>();

// Configure Gemini with debugging
builder.Services.Configure<GeminiSettings>(geminiSettings =>
{
    var config = builder.Configuration.GetSection("Gemini");
    config.Bind(geminiSettings);
    
    // Debug logging
    var logger = LoggerFactory.Create(b => b.AddConsole()).CreateLogger("Startup");
    var apiKey = geminiSettings.ApiKey;
    logger.LogInformation("🔧 Gemini API Key loaded: {HasKey}", !string.IsNullOrWhiteSpace(apiKey) ? $"Yes ({apiKey.Length} chars)" : "NO - MISSING!");
    logger.LogInformation("🔧 Gemini Model: {Model}", geminiSettings.Model);
});

builder.Services.AddHttpClient(); // Required for Gemini
builder.Services.AddScoped<GeminiInsightService>();
builder.Services.AddScoped<TestDataSeeder>();

var app = builder.Build();

// Global exception handling + custom error pages
if (app.Environment.IsDevelopment())
{
    // detailed errors (dev only)
    app.UseDeveloperExceptionPage();
}
else
{
    // routes unhandled exceptions to Razor Page at /Pages/Error.cshtml
    app.UseExceptionHandler("/Error");
    app.UseHsts();
}

// Re-execute for non-success HTTP status codes (404/403/etc.) using /Error/{code}
app.UseStatusCodePagesWithReExecute("/Error/{0}");

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();

app.UseAuthentication();
app.UseUserSync(); // Auto-create missing app profiles
app.UseAuthorization();

app.MapRazorPages();

app.MapGet("/_routes", (IEnumerable<EndpointDataSource> sources) =>
{
    var endpoints = sources.SelectMany(s => s.Endpoints)
                           .OfType<RouteEndpoint>()
                           .Select(e => new
                           {
                               Pattern = e.RoutePattern.RawText,
                               Name = e.DisplayName
                           });
    return Results.Json(endpoints);
});

// Seed data on startup
using (var scope = app.Services.CreateScope())
{
    await CampusConnect.Data.DbSeeder.SeedRolesAndAdminAsync(scope.ServiceProvider);
}

app.Run();
