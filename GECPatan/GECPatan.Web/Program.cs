using GECPatan.Core.Data;
using GECPatan.Core.Models.Domain;
using GECPatan.Web.Services;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

// ── DATABASE ─────────────────────────────────────────
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlServer(
        builder.Configuration.GetConnectionString("DefaultConnection")));
builder.Services.AddHttpClient<IDepartmentApiService, DepartmentApiService>(client =>
{
    client.BaseAddress = new Uri(builder.Configuration["Api:BaseUrl"]!);
});

// ── IDENTITY ─────────────────────────────────────────
builder.Services.AddIdentity<ApplicationUser, IdentityRole>(options =>
{
    options.Password.RequireDigit = true;
    options.Password.RequireLowercase = true;
    options.Password.RequireUppercase = true;
    options.Password.RequireNonAlphanumeric = true;
    options.Password.RequiredLength = 8;
    options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(15);
    options.Lockout.MaxFailedAccessAttempts = 5;
    options.User.RequireUniqueEmail = true;
})
.AddEntityFrameworkStores<ApplicationDbContext>()
.AddDefaultTokenProviders();

// ── COOKIE ───────────────────────────────────────────
builder.Services.ConfigureApplicationCookie(options =>
{
    options.LoginPath = "/Account/Login";
    options.LogoutPath = "/Account/Logout";
    options.AccessDeniedPath = "/Account/AccessDenied";
    options.ExpireTimeSpan = TimeSpan.FromHours(8);
    options.SlidingExpiration = true;
});

// ── MVC ──────────────────────────────────────────────
// NOTE: This is a stop-gap. GECPatan.Admin's controllers are being picked up
// because its assembly is somewhere in Web's dependency graph (directly or
// via a shared project). The real fix is finding and removing that
// <ProjectReference> — run this to locate it:
//   Get-ChildItem -Recurse -Filter *.csproj | Select-String "ProjectReference"
// Until that's cleaned up, explicitly strip Admin's ApplicationPart so its
// controllers never register with Web's routing table.
builder.Services.AddControllersWithViews()
    .ConfigureApplicationPartManager(apm =>
    {
        var partsToRemove = apm.ApplicationParts
            .Where(p => p.Name is "GECPatan.Admin")
            .ToList();

        foreach (var part in partsToRemove)
        {
            apm.ApplicationParts.Remove(part);
        }
    });

var app = builder.Build();

// ── PIPELINE ─────────────────────────────────────────
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseRouting();
app.UseAuthentication();
app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

// ── SEED ROLES + SUPERADMIN ──────────────────────────
using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;
    try
    {
        var context = services.GetRequiredService<ApplicationDbContext>();
        var userManager = services.GetRequiredService<UserManager<ApplicationUser>>();
        var roleManager = services.GetRequiredService<RoleManager<IdentityRole>>();
        var config = services.GetRequiredService<IConfiguration>();
        await context.Database.MigrateAsync();
        //await RoleSeeder.SeedAsync(userManager, roleManager, config);
        //await MenuItemSeeder.SeedAsync(context);
    }
    catch (Exception ex)
    {
        var logger = services.GetRequiredService<ILogger<Program>>();
        logger.LogError(ex, "An error occurred during migration or seeding.");
    }
}

app.Run();