using CampusConnect.Data;
using CampusConnect.Middleware;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddRazorPages();

// DbContext Registeration
builder.Services.AddDbContext<ApplicationDbContext>(options => options.UseSqlServer(builder.Configuration.GetConnectionString("ApplicationDbContext") ?? throw new InvalidOperationException("Connection string 'ApplicationDbContext' not found.")));

// Register the TablesDbContext for legacy tables
builder.Services.AddDbContext<TablesDbContext>(options => options.UseSqlServer(builder.Configuration.GetConnectionString("TablesDbContext") ?? throw new InvalidOperationException("Connection string 'TablesDbContext' not found.")));

// Register the Identity services
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

// Configure the HTTP request pipeline.
/*if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error");
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}*/

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();

app.UseAuthentication();
app.UseUserSync(); // <-- ADD THIS LINE: Auto-create missing app profiles
app.UseAuthorization();

app.MapRazorPages();


app.MapGet("/_routes", (IEnumerable<EndpointDataSource> sources) =>
{
    var endpoints = sources.SelectMany(s => s.Endpoints)
        .Select(e => e.DisplayName)
        .Where(n => n != null);

    return string.Join("\n", endpoints!);
});

//Db Seeder Resgistration
using (var scope = app.Services.CreateScope())
{
    await DbSeeder.SeedRolesAndAdminAsync(scope.ServiceProvider);
}

app.Run();
