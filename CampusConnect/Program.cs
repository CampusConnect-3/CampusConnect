using CampusConnect.Data;
using CampusConnect.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddRazorPages();

// DbContext Registration
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlServer(
        builder.Configuration.GetConnectionString("ApplicationDbContext")
        ?? throw new InvalidOperationException("Connection string 'ApplicationDbContext' not found.")));

// Register the TablesDbContext for legacy tables
builder.Services.AddDbContext<TablesDbContext>(options =>
    options.UseSqlServer(
        builder.Configuration.GetConnectionString("TablesDbContext")
        ?? throw new InvalidOperationException("Connection string 'TablesDbContext' not found.")));

// Register the Identity services
builder.Services.AddIdentity<ApplicationUser, IdentityRole>(options =>
{
    options.SignIn.RequireConfirmedAccount = true;

    // Prevent duplicate accounts by email
    options.User.RequireUniqueEmail = true;
})
.AddEntityFrameworkStores<ApplicationDbContext>()
.AddDefaultUI()
.AddDefaultTokenProviders();

// Secure session management for Identity = harden the auth cookie
builder.Services.ConfigureApplicationCookie(options =>
{
    options.Cookie.HttpOnly = true;

    // Dev-friendly: secure cookie only when request is HTTPS in Development,
    // but HTTPS-only in non-Development environments.
    options.Cookie.SecurePolicy = builder.Environment.IsDevelopment()
        ? CookieSecurePolicy.SameAsRequest
        : CookieSecurePolicy.Always;

    options.Cookie.SameSite = SameSiteMode.Lax; // Strong CSRF protection; avoids breaking common flows

    options.ExpireTimeSpan = TimeSpan.FromMinutes(30); // session lifetime
    options.SlidingExpiration = true;                  // refresh on activity

    options.LoginPath = "/Identity/Account/Login";
    options.LogoutPath = "/Identity/Account/Logout";
    options.AccessDeniedPath = "/Identity/Account/AccessDenied";
});

var app = builder.Build();

// Global exception handling (logs + routes to custom error page)
if (app.Environment.IsDevelopment())
{
    app.UseDeveloperExceptionPage(); // detailed errors (dev only)
}
else
{
    app.UseExceptionHandler("/Error"); // Razor Page at /Pages/Error.cshtml
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();

app.UseAuthentication();
app.UseAuthorization();

app.MapRazorPages();

// Db Seeder Registration (async-scope safe)
await using (var scope = app.Services.CreateAsyncScope())
{
  
    var identityDb = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
    await identityDb.Database.MigrateAsync();

    await DbSeeder.SeedRolesAndAdminAsync(scope.ServiceProvider);
}

app.Run();