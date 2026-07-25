// ── USING DIRECTIVES ───────────────────────────────── (REMOVED)
// WHY REMOVED: The web project no longer interacts directly with the database 
// or the Identity framework. All data and auth operations are now handled by 
// the external API, so these namespaces are no longer needed here.
// using GECPatan.Core.Data;
// using GECPatan.Core.Models.Domain;
// using Microsoft.AspNetCore.Identity;
// using Microsoft.EntityFrameworkCore;

using GECPatan.Web.Services;

var builder = WebApplication.CreateBuilder(args);

// ── DATABASE ───────────────────────────────────────── (REMOVED)
// WHY REMOVED: GECPatan.Web never talks to the SQL database directly anymore. 
// Removing this cuts out the SQL connection and Entity Framework migration checks 
// on startup. This was the main cause of the multi-second delay when the app 
// pool wakes up from idling.
// builder.Services.AddDbContext<ApplicationDbContext>(options =>
//     options.UseSqlServer(
//         builder.Configuration.GetConnectionString("DefaultConnection")));

builder.Services.AddHttpClient<IHomeApiService, HomeApiService>(client =>
{
    client.BaseAddress = new Uri(builder.Configuration["Api:BaseUrl"]!);
});
builder.Services.AddHttpClient<IDepartmentApiService, DepartmentApiService>(client =>
{
    client.BaseAddress = new Uri(builder.Configuration["Api:BaseUrl"]!);
});
builder.Services.AddHttpClient<IMenuApiService, MenuApiService>(client =>
{
    client.BaseAddress = new Uri(builder.Configuration["Api:BaseUrl"]!);
});
builder.Services.AddHttpClient<IDepartmentApiService, DepartmentApiService>(client =>
{
    var baseUrl = builder.Configuration["Api:BaseUrl"]
        ?? throw new InvalidOperationException("Api:BaseUrl is not configured.");

    client.BaseAddress = new Uri(baseUrl);
    client.Timeout = TimeSpan.FromSeconds(30);
    client.DefaultRequestHeaders.Add("Accept", "application/json");
});

builder.Services.AddHttpClient<IFacultyApiService, FacultyApiService>(client =>
{
    var baseUrl = builder.Configuration["Api:BaseUrl"]
        ?? throw new InvalidOperationException("Api:BaseUrl is not configured.");

    client.BaseAddress = new Uri(baseUrl);
    client.Timeout = TimeSpan.FromSeconds(30);
    client.DefaultRequestHeaders.Add("Accept", "application/json");
});
builder.Services.AddHttpClient<IFacilityApiService, FacilityApiService>(client =>
{
    var baseUrl = builder.Configuration["Api:BaseUrl"]
        ?? throw new InvalidOperationException("Api:BaseUrl is not configured.");

    client.BaseAddress = new Uri(baseUrl);
    client.Timeout = TimeSpan.FromSeconds(30);
    client.DefaultRequestHeaders.Add("Accept", "application/json");
});
builder.Services.AddHttpClient<IAchievementApiService, AchievementApiService>(client =>
{
    var baseUrl = builder.Configuration["Api:BaseUrl"]
        ?? throw new InvalidOperationException("Api:BaseUrl is not configured.");

    client.BaseAddress = new Uri(baseUrl);
    client.Timeout = TimeSpan.FromSeconds(30);
    client.DefaultRequestHeaders.Add("Accept", "application/json");
});
builder.Services.AddHttpClient<ITenderApiService, TenderApiService>(client =>
{
    var baseUrl = builder.Configuration["Api:BaseUrl"]
        ?? throw new InvalidOperationException("Api:BaseUrl is not configured.");
    client.BaseAddress = new Uri(baseUrl);
    client.Timeout = TimeSpan.FromSeconds(30);
    client.DefaultRequestHeaders.Add("Accept", "application/json");
});
builder.Services.AddHttpClient<IContentPageApiService, ContentPageApiService>(client =>
{
    var baseUrl = builder.Configuration["Api:BaseUrl"]
        ?? throw new InvalidOperationException("Api:BaseUrl is not configured.");

    client.BaseAddress = new Uri(baseUrl);
    client.Timeout = TimeSpan.FromSeconds(30);
    client.DefaultRequestHeaders.Add("Accept", "application/json");
});

builder.Services.AddHttpClient<INewsApiService, NewsApiService>(client =>
   {
       var baseUrl = builder.Configuration["Api:BaseUrl"]
           ?? throw new InvalidOperationException("Api:BaseUrl is not configured.");

       client.BaseAddress = new Uri(baseUrl);
       client.Timeout = TimeSpan.FromSeconds(30);
       client.DefaultRequestHeaders.Add("Accept", "application/json");
   });

builder.Services.AddHttpClient<IStudentClubApiService, StudentClubApiService>(client =>
{
    var baseUrl = builder.Configuration["Api:BaseUrl"]
        ?? throw new InvalidOperationException("Api:BaseUrl is not configured.");

    client.BaseAddress = new Uri(baseUrl);
    client.Timeout = TimeSpan.FromSeconds(30);
    client.DefaultRequestHeaders.Add("Accept", "application/json");
});



// ── IDENTITY ───────────────────────────────────────── (REMOVED)
// WHY REMOVED: User management, password policies, and lockout rules are now 
// enforced entirely by the API layer. The frontend UI just consumes the API 
// and doesn't need to know the underlying Identity configuration.
// builder.Services.AddIdentity<ApplicationUser, IdentityRole>(options =>
// {
//     options.Password.RequireDigit = true;
//     options.Password.RequireLowercase = true;
//     options.Password.RequireUppercase = true;
//     options.Password.RequireNonAlphanumeric = true;
//     options.Password.RequiredLength = 8;
//     options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(15);
//     options.Lockout.MaxFailedAccessAttempts = 5;
//     options.User.RequireUniqueEmail = true;
// })
// .AddEntityFrameworkStores<ApplicationDbContext>()
// .AddDefaultTokenProviders();

// ── COOKIE ─────────────────────────────────────────── (REMOVED)
// WHY REMOVED: Authentication state is no longer managed by local MVC cookies 
// tied to the database. The Web app likely relies on API tokens (like JWTs) 
// passed via the HTTP clients now.
// builder.Services.ConfigureApplicationCookie(options =>
// {
//     options.LoginPath = "/Account/Login";
//     options.LogoutPath = "/Account/Logout";
//     options.AccessDeniedPath = "/Account/AccessDenied";
//     options.ExpireTimeSpan = TimeSpan.FromHours(8);
//     options.SlidingExpiration = true;
// });

// ── MVC ──────────────────────────────────────────────
// (NOTE: This part was kept to prevent Admin controllers from bleeding into Web routing)
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
else
{
    app.UseDeveloperExceptionPage();
}

app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseRouting();

// ── AUTHENTICATION MIDDLEWARE ──────────────────────── (REMOVED)
// WHY REMOVED: Since local Identity and Cookie auth configurations were removed 
// above, the local authentication middleware is no longer needed.
// app.UseAuthentication();

app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

// ── SEED ROLES + SUPERADMIN ────────────────────────── (REMOVED)
// WHY REMOVED: The UI project should never be responsible for migrating or 
// seeding the database. Moving this out of the startup path dramatically improves 
// boot speed. It also prevents concurrent migration crashes if you ever scale 
// this app to run on multiple servers at the same time.
// using (var scope = app.Services.CreateScope())
// {
//     var services = scope.ServiceProvider;
//     try
//     {
//         var context = services.GetRequiredService<ApplicationDbContext>();
//         var userManager = services.GetRequiredService<UserManager<ApplicationUser>>();
//         var roleManager = services.GetRequiredService<RoleManager<IdentityRole>>();
//         var config = services.GetRequiredService<IConfiguration>();
//         await context.Database.MigrateAsync();
//         //await RoleSeeder.SeedAsync(userManager, roleManager, config);
//         //await MenuItemSeeder.SeedAsync(context);
//     }
//     catch (Exception ex)
//     {
//         var logger = services.GetRequiredService<ILogger<Program>>();
//         logger.LogError(ex, "An error occurred during migration or seeding.");
//     }
// }

app.Run();