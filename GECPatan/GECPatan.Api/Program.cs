using GECPatan.Admin.Data;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

// ── DATABASE ──────────────────────────────────────────
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlServer(
        builder.Configuration
            .GetConnectionString("DefaultConnection")));

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

// ── CONTROLLERS ───────────────────────────────────────
builder.Services.AddControllers();

// ── SWAGGER ───────────────────────────────────────────
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new()
    {
        Title = "GEC Patan API",
        Version = "v1",
        Description = "Public API for GEC Patan Website"
    });
});

// ── ROUTING (lowercase URLs) ──────────────────────────
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
        c.SwaggerEndpoint("/swagger/v1/swagger.json",
            "GEC Patan API v1");
        c.RoutePrefix = "swagger";
    });
}

app.UseHttpsRedirection();
app.UseCors("WebFrontend");
app.MapControllers();

app.Run();