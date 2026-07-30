using GECPatan.Core.Data;
using GECPatan.Core.Models.Domain;
using GECPatan.Core.Services;
using GECPatan.Core.Services.FileStorage;
using GECPatan.Core.Services.UserManagement;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

var builder = WebApplication.CreateBuilder(args);

// ── DATABASE ──────────────────────────────────────────
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlServer(
        builder.Configuration.GetConnectionString("DefaultConnection")));

// ── IDENTITY ──────────────────────────────────────────
builder.Services.AddIdentity<ApplicationUser, IdentityRole>(options =>
{
    // Password
    options.Password.RequireDigit = true;
    options.Password.RequireLowercase = true;
    options.Password.RequireUppercase = true;
    options.Password.RequireNonAlphanumeric = true;
    options.Password.RequiredLength = 8;

    // Lockout
    options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(15);
    options.Lockout.MaxFailedAccessAttempts = 5;
    options.Lockout.AllowedForNewUsers = true;

    // User
    options.User.RequireUniqueEmail = true;

    // Sign in
    options.SignIn.RequireConfirmedEmail = false;
    options.SignIn.RequireConfirmedAccount = false;
})
.AddEntityFrameworkStores<ApplicationDbContext>()
.AddDefaultTokenProviders();

// ── COOKIE ────────────────────────────────────────────
builder.Services.ConfigureApplicationCookie(options =>
{
    options.LoginPath = "/Account/Login";
    options.LogoutPath = "/Account/Logout";
    options.AccessDeniedPath = "/Account/AccessDenied";
    options.ExpireTimeSpan = TimeSpan.FromHours(1);
    options.SlidingExpiration = true;
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
});
//Notification
builder.Services.AddScoped<NotificationService>();
builder.Services.AddHttpContextAccessor();
//Audit-Log
builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<GECPatan.Core.Services.AuditService>();
// ── MVC ───────────────────────────────────────────────
builder.Services.AddControllersWithViews();
//DIscloure Data Service
builder.Services.AddScoped<
    GECPatan.Admin.Services.DisclosureDataService>();

// ── FILE STORAGE ──────────────────────────────────────
// Blob-storage-backed implementation, targeting Azurite locally (see
// docker-compose.yml at the repo root — `docker compose up -d azurite`)
// and a real Azure Storage account in production via the same
// "BlobStorage" configuration section (just swap the connection string).
//
// The original disk-based FileStorageService (wwwroot/uploads) is left
// completely intact in the codebase — only this registration changed.
// Every controller depends on IFileStorageService, not a concrete class,
// so no controller changes were needed to make this swap.
builder.Services.Configure<BlobStorageOptions>(
    builder.Configuration.GetSection(BlobStorageOptions.SectionName));

builder.Services.AddScoped<IFileStorageService>(sp =>
{
    var options = sp.GetRequiredService<IOptions<BlobStorageOptions>>().Value;
    return new BlobStorageService(
        options.ConnectionString,
        options.ContainerName,
        options.PublicBaseUrl,
        sp.GetRequiredService<ILogger<BlobStorageService>>());
});

// Disk-based alternative — kept here, commented, for an easy rollback.
// To switch back, comment out the BlobStorageService registration above
// and uncomment this block instead. No other code changes are required
// either way, since both implement the same IFileStorageService interface.
//
// builder.Services.AddScoped<IFileStorageService>(sp =>
//     new FileStorageService(
//         sp.GetRequiredService<IWebHostEnvironment>().WebRootPath,
//         sp.GetRequiredService<ILogger<FileStorageService>>()));

// Batch-loading lookups for the User Management list page — see
// UserDirectoryService for why this exists (fixes an N+1 query pattern).
builder.Services.AddScoped<UserDirectoryService>();

var app = builder.Build();

// ── PIPELINE ──────────────────────────────────────────
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseRouting();

// ORDER MATTERS: Authentication before Authorization
app.UseAuthentication();
app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Account}/{action=Login}/{id?}");

// ── SEED ──────────────────────────────────────────────
using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;

    try
    {
        var context = services.GetRequiredService<ApplicationDbContext>();
        var userManager = services.GetRequiredService<UserManager<ApplicationUser>>();
        var roleManager = services.GetRequiredService<RoleManager<IdentityRole>>();
        var config = services.GetRequiredService<IConfiguration>();
        var db = scope.ServiceProvider.GetRequiredService<GECPatan.Core.Data.ApplicationDbContext>();

        await context.Database.MigrateAsync();
        await RoleSeeder.SeedAsync(userManager, roleManager, config);
        await GECPatan.Admin.Data.DisclosureNarrativeSeeder.SeedAsync(db);
    }
    catch (Exception ex)
    {
        var logger = services.GetRequiredService<ILogger<Program>>();
        logger.LogError(ex, "Error during seeding.");
    }
}

app.Run();
