using GECPatan.Admin.Data;
using GECPatan.Admin.Models.Domain;
using GECPatan.Admin.Models.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;

namespace GECPatan.Admin.Controllers
{
    [Authorize(Roles = "SuperAdmin,HOD,ContentEditor,Principal")]
    public class AchievementController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly IWebHostEnvironment _env;

        public AchievementController(ApplicationDbContext context, IWebHostEnvironment env)
        {
            _context = context;
            _env = env;
        }

        // ── INDEX ──
        public async Task<IActionResult> Index(int? deptId, int? committeeId, int? year)
        {
            ViewData["Title"] = "Achievements";
            ViewBag.Departments = new SelectList(await _context.Departments.OrderBy(d => d.Name).ToListAsync(), "DeptId", "Name", deptId);
            ViewBag.Committees = new SelectList(await _context.CampusCommittees.OrderBy(c => c.Title).ToListAsync(), "Id", "Title", committeeId);

            var years = await _context.Achievements.Where(a => a.Year.HasValue).Select(a => a.Year!.Value).Distinct().OrderByDescending(y => y).ToListAsync();
            ViewBag.Years = new SelectList(years.Select(y => new { Value = y, Text = y.ToString() }), "Value", "Text", year);
            ViewBag.SelectedYear = year;

            var query = _context.Achievements.AsQueryable();
            if (deptId.HasValue) query = query.Where(a => a.DeptId == deptId);
            if (committeeId.HasValue) query = query.Where(a => a.CommitteeId == committeeId);
            if (year.HasValue) query = query.Where(a => a.Year == year);

            var list = await query
                .OrderByDescending(a => a.Date)
                .Select(a => new AchievementListVM
                {
                    Id = a.Id,
                    Title = a.Title,
                    Date = a.Date,
                    DeptName = a.DeptName,
                    Year = a.Year,
                    IsVisible = a.IsVisible,
                    ImagePath = a.ImagePath
                })
                .ToListAsync();

            return View(list);
        }

        // ── CREATE GET ───
        public async Task<IActionResult> Create()
        {
            ViewData["Title"] = "Add Achievement";
            return View(await BuildCreateVM(new AchievementCreateVM()));
        }

        // ── CREATE POST ───
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(AchievementCreateVM model, IFormFile? Image)
        {
            ViewData["Title"] = "Add Achievement";
            if (!ModelState.IsValid)
                return View(await BuildCreateVM(model));

            var ach = new Achievement
            {
                Title = model.Title,
                Description = model.Description,
                Date = model.Date,
                Year = model.Year,
                Keywords = model.Keywords,
                Type = model.Type,
                DeptId = model.DeptId,
                DeptName = model.DeptName,
                CommitteeId = model.CommitteeId,
                IsVisible = model.IsVisible
            };

            if (Image != null && Image.Length > 0)
                ach.ImagePath = await SaveFileAsync(Image, "achievements");

            _context.Achievements.Add(ach);
            await _context.SaveChangesAsync();

            TempData["Success"] = "Achievement added.";
            return RedirectToAction(nameof(Index));
        }

        // ── EDIT GET ───
        public async Task<IActionResult> Edit(int id)
        {
            ViewData["Title"] = "Edit Achievement";
            var a = await _context.Achievements.FindAsync(id);
            if (a == null) return NotFound();

            var vm = new AchievementEditVM
            {
                Id = a.Id,
                Title = a.Title,
                Description = a.Description,
                Date = a.Date,
                Year = a.Year,
                Keywords = a.Keywords,
                Type = a.Type,
                DeptId = a.DeptId,
                DeptName = a.DeptName,
                CommitteeId = a.CommitteeId,
                IsVisible = a.IsVisible,
                ExistingImagePath = a.ImagePath
            };

            return View(await BuildEditVM(vm));
        }

        // ── EDIT POST ----
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, AchievementEditVM model, IFormFile? Image)
        {
            ViewData["Title"] = "Edit Achievement";
            if (!ModelState.IsValid)
                return View(await BuildEditVM(model));

            var a = await _context.Achievements.FindAsync(id);
            if (a == null) return NotFound();

            a.Title = model.Title;
            a.Description = model.Description;
            a.Date = model.Date;
            a.Year = model.Year;
            a.Keywords = model.Keywords;
            a.Type = model.Type;
            a.DeptId = model.DeptId;
            a.DeptName = model.DeptName;
            a.CommitteeId = model.CommitteeId;
            a.IsVisible = model.IsVisible;

            if (Image != null && Image.Length > 0)
            {
                DeleteFileFromDisk(a.ImagePath);
                a.ImagePath = await SaveFileAsync(Image, "achievements");
            }

            await _context.SaveChangesAsync();
            TempData["Success"] = "Achievement updated.";
            return RedirectToAction(nameof(Index));
        }

        // ── TOGGLE VISIBLE ────────────────────────────────
        [HttpPost]
        public async Task<IActionResult> ToggleVisible(int id)
        {
            var a = await _context.Achievements.FindAsync(id);
            if (a == null) return NotFound();
            a.IsVisible = !a.IsVisible;
            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        // ── DELETE ────────────────────────────────────────
        [HttpPost]
        [Authorize(Roles = "SuperAdmin,HOD")]
        public async Task<IActionResult> Delete(int id)
        {
            var a = await _context.Achievements.FindAsync(id);
            if (a == null) return NotFound();
            a.IsDeleted = true;
            await _context.SaveChangesAsync();
            TempData["Success"] = "Achievement deleted.";
            return RedirectToAction(nameof(Index));
        }

        // ── HELPERS ───────────────────────────────────────
        private async Task<AchievementCreateVM> BuildCreateVM(AchievementCreateVM vm)
        {
            vm.Departments = await _context.Departments.OrderBy(d => d.Name)
                .Select(d => new SelectListItem { Value = d.DeptId.ToString(), Text = d.Name }).ToListAsync();
            vm.Committees = await _context.CampusCommittees.OrderBy(c => c.Title)
                .Select(c => new SelectListItem { Value = c.Id.ToString(), Text = c.Title }).ToListAsync();
            vm.Types = new List<SelectListItem>
            {
                new("Academic", "1"), new("Sports", "2"),
                new("Cultural", "3"), new("NSS", "4"), new("Other", "5")
            };
            return vm;
        }

        private async Task<AchievementEditVM> BuildEditVM(AchievementEditVM vm)
        {
            vm.Departments = await _context.Departments.OrderBy(d => d.Name)
                .Select(d => new SelectListItem { Value = d.DeptId.ToString(), Text = d.Name }).ToListAsync();
            vm.Committees = await _context.CampusCommittees.OrderBy(c => c.Title)
                .Select(c => new SelectListItem { Value = c.Id.ToString(), Text = c.Title }).ToListAsync();
            vm.Types = new List<SelectListItem>
            {
                new("Academic", "1"), new("Sports", "2"),
                new("Cultural", "3"), new("NSS", "4"), new("Other", "5")
            };
            return vm;
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

        private void DeleteFileFromDisk(string? filePath)
        {
            if (string.IsNullOrEmpty(filePath)) return;
            var fullPath = Path.Combine(_env.WebRootPath, filePath.TrimStart('/'));
            if (System.IO.File.Exists(fullPath)) System.IO.File.Delete(fullPath);
        }
    }
}