using GECPatan.Core.Data;
using GECPatan.Core.Models.Domain;
using GECPatan.Admin.Models.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace GECPatan.Admin.Controllers
{
    [Authorize(Roles = "SuperAdmin,ContentEditor,PlacementOfficer")]
    public class TopRecruiterController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly IWebHostEnvironment _env;

        public TopRecruiterController(ApplicationDbContext context, IWebHostEnvironment env)
        {
            _context = context;
            _env = env;
        }

        public async Task<IActionResult> Index()
        {
            ViewData["Title"] = "Top Recruiters";
            var items = await _context.TopRecruiters
                .OrderBy(r => r.DisplayOrder)
                .ToListAsync();
            return View(items);
        }

        public IActionResult Create()
        {
            ViewData["Title"] = "Add Recruiter";
            return View(new TopRecruiterVM());
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(TopRecruiterVM model, IFormFile? Logo)
        {
            ViewData["Title"] = "Add Recruiter";
            if (!ModelState.IsValid) return View(model);

            int maxOrder = await _context.TopRecruiters.Select(r => (int?)r.DisplayOrder).MaxAsync() ?? -1;

            var recruiter = new TopRecruiter
            {
                Name = model.Name,
                DisplayOrder = maxOrder + 1,
                IsVisible = model.IsVisible
            };

            if (Logo != null && Logo.Length > 0)
                recruiter.LogoPath = await SaveFileAsync(Logo, "recruiters");

            _context.TopRecruiters.Add(recruiter);
            await _context.SaveChangesAsync();

            TempData["Success"] = "Recruiter added.";
            return RedirectToAction(nameof(Index));
        }

        public async Task<IActionResult> Edit(int id)
        {
            ViewData["Title"] = "Edit Recruiter";
            var r = await _context.TopRecruiters.FindAsync(id);
            if (r == null) return NotFound();

            return View(new TopRecruiterVM
            {
                Id = r.Id,
                Name = r.Name,
                DisplayOrder = r.DisplayOrder,
                IsVisible = r.IsVisible,
                ExistingLogoPath = r.LogoPath
            });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, TopRecruiterVM model, IFormFile? Logo)
        {
            ViewData["Title"] = "Edit Recruiter";
            if (!ModelState.IsValid) return View(model);

            var r = await _context.TopRecruiters.FindAsync(id);
            if (r == null) return NotFound();

            r.Name = model.Name;
            r.IsVisible = model.IsVisible;

            if (Logo != null && Logo.Length > 0)
            {
                DeleteFile(r.LogoPath);
                r.LogoPath = await SaveFileAsync(Logo, "recruiters");
            }

            await _context.SaveChangesAsync();
            TempData["Success"] = "Recruiter updated.";
            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        public async Task<IActionResult> ToggleVisible(int id)
        {
            var r = await _context.TopRecruiters.FindAsync(id);
            if (r == null) return NotFound();
            r.IsVisible = !r.IsVisible;
            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        public async Task<IActionResult> Delete(int id)
        {
            var r = await _context.TopRecruiters.FindAsync(id);
            if (r == null) return NotFound();
            DeleteFile(r.LogoPath);
            r.IsDeleted = true;
            await _context.SaveChangesAsync();
            TempData["Success"] = "Recruiter deleted.";
            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        public async Task<IActionResult> Reorder(int id, string direction)
        {
            var r = await _context.TopRecruiters.FindAsync(id);
            if (r == null) return NotFound();

            if (direction == "up")
            {
                var above = await _context.TopRecruiters.Where(x => x.DisplayOrder == r.DisplayOrder - 1).FirstOrDefaultAsync();
                if (above != null) { above.DisplayOrder++; r.DisplayOrder--; }
            }
            else
            {
                var below = await _context.TopRecruiters.Where(x => x.DisplayOrder == r.DisplayOrder + 1).FirstOrDefaultAsync();
                if (below != null) { below.DisplayOrder--; r.DisplayOrder++; }
            }

            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
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

        private void DeleteFile(string? filePath)
        {
            if (string.IsNullOrEmpty(filePath)) return;
            var fullPath = Path.Combine(_env.WebRootPath, filePath.TrimStart('/'));
            if (System.IO.File.Exists(fullPath)) System.IO.File.Delete(fullPath);
        }
    }
}