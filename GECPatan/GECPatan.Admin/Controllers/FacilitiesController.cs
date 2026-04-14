using GECPatan.Admin.Data;
using GECPatan.Admin.Models.Domain;
using GECPatan.Admin.Models.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace GECPatan.Admin.Controllers
{
    [Authorize(Roles = "SuperAdmin,ContentEditor")]
    public class FacilityController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly IWebHostEnvironment _env;

        // Facility types managed here
        private static readonly string[] FacilityTypes = new[]
        {
            "Hostel", "Library", "Medical", "Transportation",
            "Gymkhana", "Auditorium", "Canteen", "StudentSection",
            "CentreOfExcellence", "ISRO"
        };

        public FacilityController(ApplicationDbContext context, IWebHostEnvironment env)
        {
            _context = context;
            _env = env;
        }

        public async Task<IActionResult> Index()
        {
            ViewData["Title"] = "Facilities";

            // Show all facility types — create if not exists
            var facilities = await _context.SiteSettings
                .Where(s => s.Group == "Facility")
                .ToListAsync();

            // Build list of facility pages
            var list = FacilityTypes.Select(ft => new
            {
                Type = ft,
                Display = System.Text.RegularExpressions.Regex.Replace(ft, "([A-Z])", " $1").Trim(),
                HasData = facilities.Any(f => f.Key == $"Facility_{ft}_Title")
            }).ToList();

            ViewBag.FacilityList = list;
            return View();
        }

        public async Task<IActionResult> Edit(string type)
        {
            ViewData["Title"] = $"Edit {type} Page";

            // Load settings for this facility
            string prefix = $"Facility_{type}_";
            var settings = await _context.SiteSettings
                .Where(s => s.Key.StartsWith(prefix))
                .ToDictionaryAsync(s => s.Key, s => s.Value);

            var vm = new FacilityVM
            {
                FacilityType = type,
                Title = settings.GetValueOrDefault($"{prefix}Title", type),
                Tagline = settings.GetValueOrDefault($"{prefix}Tagline"),
                About = settings.GetValueOrDefault($"{prefix}About"),
                VisionItems = settings.GetValueOrDefault($"{prefix}Vision"),
                MissionItems = settings.GetValueOrDefault($"{prefix}Mission"),
                ExistingTitleImagePath = settings.GetValueOrDefault($"{prefix}TitleImage")
            };

            // Load dynamic sections count
            ViewBag.SectionCount = await _context.DynamicSections
                .Where(s => s.PageType == PageType.Facility &&
                            s.PageId == GetFacilityId(type))
                .CountAsync();
            ViewBag.FacilityId = GetFacilityId(type);

            return View(vm);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(string type, FacilityVM model, IFormFile? TitleImage)
        {
            ViewData["Title"] = $"Edit {type} Page";
            if (!ModelState.IsValid) return View(model);

            string prefix = $"Facility_{type}_";

            await UpsertSetting($"{prefix}Title", model.Title, "Facility");
            await UpsertSetting($"{prefix}Tagline", model.Tagline, "Facility");
            await UpsertSetting($"{prefix}About", model.About, "Facility");
            await UpsertSetting($"{prefix}Vision", model.VisionItems, "Facility");
            await UpsertSetting($"{prefix}Mission", model.MissionItems, "Facility");

            if (TitleImage != null && TitleImage.Length > 0)
            {
                var path = await SaveFileAsync(TitleImage, "facilities");
                await UpsertSetting($"{prefix}TitleImage", path, "Facility");
            }

            await _context.SaveChangesAsync();
            TempData["Success"] = $"{type} page updated.";
            return RedirectToAction(nameof(Index));
        }

        // ── HELPERS ───────────────────────────────────────
        private async Task UpsertSetting(string key, string? value, string group)
        {
            var existing = await _context.SiteSettings
                .FirstOrDefaultAsync(s => s.Key == key);

            if (existing == null)
            {
                _context.SiteSettings.Add(new SiteSetting
                {
                    Key = key,
                    Value = value,
                    Group = group
                });
            }
            else
            {
                existing.Value = value;
            }
        }

        private static int GetFacilityId(string type) => type switch
        {
            "Hostel" => 1001,
            "Library" => 1002,
            "Medical" => 1003,
            "Transportation" => 1004,
            "Gymkhana" => 1005,
            "Auditorium" => 1006,
            "Canteen" => 1007,
            "StudentSection" => 1008,
            "CentreOfExcellence" => 1009,
            "ISRO" => 1010,
            _ => 1000
        };

        private async Task<string> SaveFileAsync(IFormFile file, string folder)
        {
            var uploadsFolder = Path.Combine(_env.WebRootPath, "uploads", folder);
            Directory.CreateDirectory(uploadsFolder);
            var fileName = Guid.NewGuid() + Path.GetExtension(file.FileName);
            var filePath = Path.Combine(uploadsFolder, fileName);
            using var stream = new FileStream(filePath, FileMode.Create);
            await file.CopyToAsync(stream);
            return $"/uploads/{folder}/{fileName}";
        }
    }
}