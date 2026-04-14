using GECPatan.Admin.Data;
using GECPatan.Admin.Models.Domain;
using GECPatan.Admin.Models.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace GECPatan.Admin.Controllers
{
    [Authorize(Roles = "SuperAdmin,Principal,ContentEditor")]
    public class PrincipalController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly IWebHostEnvironment _env;

        public PrincipalController(ApplicationDbContext context, IWebHostEnvironment env)
        {
            _context = context;
            _env = env;
        }

        public async Task<IActionResult> Index()
        {
            ViewData["Title"] = "Principal Profile";

            // Load from SiteSettings group = "Principal"
            var settings = await _context.SiteSettings
                .Where(s => s.Group == "Principal")
                .ToDictionaryAsync(s => s.Key, s => s.Value);

            var vm = new PrincipalVM
            {
                Name = settings.GetValueOrDefault("Principal_Name", ""),
                Designation = settings.GetValueOrDefault("Principal_Designation"),
                Qualification = settings.GetValueOrDefault("Principal_Qualification"),
                Email = settings.GetValueOrDefault("Principal_Email"),
                Contact = settings.GetValueOrDefault("Principal_Contact"),
                Message = settings.GetValueOrDefault("Principal_Message"),
                About = settings.GetValueOrDefault("Principal_About"),
                ExperienceText = settings.GetValueOrDefault("Principal_Experience"),
                ResearchInterests = settings.GetValueOrDefault("Principal_Research"),
                Publications = settings.GetValueOrDefault("Principal_Publications"),
                ExistingImagePath = settings.GetValueOrDefault("Principal_Image")
            };

            return View(vm);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Index(PrincipalVM model, IFormFile? Photo)
        {
            ViewData["Title"] = "Principal Profile";
            if (!ModelState.IsValid) return View(model);

            await UpsertSetting("Principal_Name", model.Name);
            await UpsertSetting("Principal_Designation", model.Designation);
            await UpsertSetting("Principal_Qualification", model.Qualification);
            await UpsertSetting("Principal_Email", model.Email);
            await UpsertSetting("Principal_Contact", model.Contact);
            await UpsertSetting("Principal_Message", model.Message);
            await UpsertSetting("Principal_About", model.About);
            await UpsertSetting("Principal_Experience", model.ExperienceText);
            await UpsertSetting("Principal_Research", model.ResearchInterests);
            await UpsertSetting("Principal_Publications", model.Publications);

            if (Photo != null && Photo.Length > 0)
            {
                var path = await SaveFileAsync(Photo, "principal");
                await UpsertSetting("Principal_Image", path);
            }

            await _context.SaveChangesAsync();
            TempData["Success"] = "Principal profile updated.";
            return RedirectToAction(nameof(Index));
        }

        private async Task UpsertSetting(string key, string? value)
        {
            var s = await _context.SiteSettings.FirstOrDefaultAsync(x => x.Key == key);
            if (s == null)
                _context.SiteSettings.Add(new SiteSetting { Key = key, Value = value, Group = "Principal" });
            else
                s.Value = value;
        }

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
