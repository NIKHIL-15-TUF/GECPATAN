using GECPatan.Core.Data;
using GECPatan.Core.Services.Email;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Options;
using System.Threading.RateLimiting;

var builder = WebApplication.CreateBuilder(args);

// ── DATABASE ──────────────────────────────────────────
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlServer(
        builder.Configuration.GetConnectionString("DefaultConnection")));

// ── CORS ──────────────────────────────────────────────
var allowedOrigins = builder.Configuration
    .GetSection("Cors:AllowedOrigins")
    .Get<string[]>() ?? Array.Empty<string>();

builder.Services.AddCors(options =>
{
    options.AddPolicy("WebFrontend", policy =>
    {
        policy
            .WithOrigins(allowedOrigins)
            .AllowAnyHeader()
            .AllowAnyMethod();
    });
});

// ── EMAIL (Contact Us notification) ────────────────────
// Shared IEmailService (GECPatan.Core.Services.Email) — also registered in
// GECPatan.Admin for the Contact Message reply feature. Transport
// credentials come from the "Smtp" appsettings section (see
// appsettings.Contact.md); the actual From/To addresses used for Contact Us
// mail are admin-editable SiteSettings, not config, so they never need a
// redeploy to change.
builder.Services.Configure<EmailOptions>(
    builder.Configuration.GetSection(EmailOptions.SectionName));
builder.Services.AddScoped<IEmailService>(sp =>
    new EmailService(
        sp.GetRequiredService<IOptions<EmailOptions>>().Value,
        sp.GetRequiredService<ILogger<EmailService>>()));

// ── RATE LIMITING (Contact Us submissions) ─────────────
// Fixed-window limiter, partitioned per client IP, so one visitor spamming
// the form can't consume the whole app's quota. Limits are configurable via
// the "RateLimiting:ContactForm" appsettings section — see
// appsettings.Contact.md — with sane defaults if that section is absent.
var contactPermitLimit = builder.Configuration.GetValue("RateLimiting:ContactForm:PermitLimit", 5);
var contactWindowMinutes = builder.Configuration.GetValue("RateLimiting:ContactForm:WindowMinutes", 10);

builder.Services.AddRateLimiter(options =>
{
    options.AddPolicy("ContactForm", httpContext =>
        RateLimitPartition.GetFixedWindowLimiter(
            partitionKey: httpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown",
            factory: _ => new FixedWindowRateLimiterOptions
            {
                PermitLimit = contactPermitLimit,
                Window = TimeSpan.FromMinutes(contactWindowMinutes),
                QueueLimit = 0,
                QueueProcessingOrder = QueueProcessingOrder.OldestFirst
            }));

    options.OnRejected = async (context, ct) =>
    {
        context.HttpContext.Response.StatusCode = StatusCodes.Status429TooManyRequests;
        context.HttpContext.Response.ContentType = "application/json";
        await context.HttpContext.Response.WriteAsJsonAsync(
            GECPatan.Api.Common.ApiResponse<object>.Fail(
                "Too many requests. Please wait a while before submitting again."),
            ct);
    };
});

// ── CONTROLLERS ───────────────────────────────────────
builder.Services.AddControllers()
    .AddJsonOptions(options =>
    {
        // Avoid circular reference issues with EF navigation props
        options.JsonSerializerOptions.ReferenceHandler =
            System.Text.Json.Serialization.ReferenceHandler.IgnoreCycles;
    });

// ── SWAGGER ───────────────────────────────────────────
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new()
    {
        Title = "GEC Patan API",
        Version = "v1",
        Description = "Public API for GEC Patan Website (GECPatan.Web)"
    });
});

// ── LOWERCASE URLS ─────────────────────────────────────
builder.Services.AddRouting(options =>
{
    options.LowercaseUrls = true;
});

var app = builder.Build();

// ── PIPELINE ──────────────────────────────────────────
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(c =>
    {
        c.SwaggerEndpoint("/swagger/v1/swagger.json", "GEC Patan API v1");
        c.RoutePrefix = "swagger";
    });
}

app.UseHttpsRedirection();
app.UseCors("WebFrontend");
app.UseRateLimiter();
var adminWwwRoot = Path.Combine(
    builder.Environment.ContentRootPath,
    "..",
    "GECPatan.Admin",
    "wwwroot");

if (Directory.Exists(adminWwwRoot))
{
    app.UseStaticFiles(new StaticFileOptions
    {
        FileProvider = new PhysicalFileProvider(adminWwwRoot),
        RequestPath = ""
    });
}
// Serve uploaded files (images/PDFs) — point to Admin's wwwroot
app.UseStaticFiles();

app.MapControllers();

app.Run();