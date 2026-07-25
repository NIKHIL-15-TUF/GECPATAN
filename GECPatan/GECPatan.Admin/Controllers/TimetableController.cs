using GECPatan.Core.Data;
using GECPatan.Core.Models.Domain;
using GECPatan.Admin.Models.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;

namespace GECPatan.Admin.Controllers
{
    [Authorize(Roles = "SuperAdmin,HOD,ContentEditor")]
    public class TimetableController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly IWebHostEnvironment _env;

        public TimetableController(ApplicationDbContext context, IWebHostEnvironment env)
        {
            _context = context;
            _env = env;
        }

        // ── INDEX ─────────────────────────────────────────
        public async Task<IActionResult> Index(int? deptId, int? year)
        {
            ViewData["Title"] = "Timetables";
            ViewBag.Departments = new SelectList(
                await _context.Departments.OrderBy(d => d.Name).ToListAsync(),
                "DeptId", "Name", deptId);
            ViewBag.SelectedDeptId = deptId;

            // Available years
            var years = await _context.Timetables
                .Select(t => t.Year).Distinct()
                .OrderByDescending(y => y).ToListAsync();

            if (!years.Contains(DateTime.Today.Year))
                years.Insert(0, DateTime.Today.Year);

            ViewBag.Years = new SelectList(years, year);
            ViewBag.SelectedYear = year;

            var query = _context.Timetables
                .Include(t => t.Department)
                .AsQueryable();

            if (User.IsInRole(AppRoles.HOD))
            {
                var cu = await _context.Users
                    .FirstOrDefaultAsync(u => u.UserName == User.Identity!.Name);
                if (cu?.DeptId != null)
                    query = query.Where(t => t.DeptId == cu.DeptId);
            }
            else if (deptId.HasValue)
                query = query.Where(t => t.DeptId == deptId.Value);

            if (year.HasValue)
                query = query.Where(t => t.Year == year.Value);

            var list = await query
                .OrderByDescending(t => t.UploadedDate)
                .Select(t => new TimetableListVM
                {
                    Id = t.Id,
                    DeptName = t.Department != null ? t.Department.Name : "",
                    DeptId = t.DeptId,
                    Year = t.Year,
                    Semester = t.Semester,
                    SemesterType = t.SemesterType,
                    FilePath = t.FilePath,
                    IsVisible = t.IsVisible,
                    IsLatest = t.IsLatest,
                    UploadedDate = t.UploadedDate
                })
                .ToListAsync();

            return View(list);
        }

        // ── CREATE ────────────────────────────────────────
        public async Task<IActionResult> Create(int? deptId)
        {
            ViewData["Title"] = "Upload Timetable";
            var vm = new TimetableCreateVM { DeptId = deptId ?? 0 };
            return View(await BuildVM(vm));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(
            TimetableCreateVM model, IFormFile? TimetableFile)
        {
            ViewData["Title"] = "Upload Timetable";
            if (!ModelState.IsValid)
                return View(await BuildVM(model));

            if (TimetableFile == null || TimetableFile.Length == 0)
            {
                ModelState.AddModelError("", "Please select a file to upload.");
                return View(await BuildVM(model));
            }

            // Mark previous timetable for same dept+sem as not latest
            var previous = await _context.Timetables
                .Where(t => t.DeptId == model.DeptId &&
                            t.Semester == model.Semester &&
                            t.Year == model.Year &&
                            t.IsLatest)
                .ToListAsync();

            foreach (var p in previous)
                p.IsLatest = false;

            var tt = new Timetable
            {
                DeptId = model.DeptId,
                Year = model.Year,
                SemesterType = model.SemesterType,
                Semester = model.Semester,
                IsVisible = model.IsVisible,
                IsLatest = true,
                UploadedDate = DateTime.Now,
                UploadedBy = User.Identity?.Name
            };

            tt.FilePath = await SaveFileAsync(TimetableFile, "timetables");

            _context.Timetables.Add(tt);
            await _context.SaveChangesAsync();

            TempData["Success"] = "Timetable uploaded successfully.";
            return RedirectToAction(nameof(Index),
                new { deptId = model.DeptId });
        }

        // ── TOGGLE VISIBLE ────────────────────────────────
        [HttpPost]
        public async Task<IActionResult> ToggleVisible(int id)
        {
            var tt = await _context.Timetables.FindAsync(id);
            if (tt == null) return NotFound();
            tt.IsVisible = !tt.IsVisible;
            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        // ── DELETE (only non-latest can be deleted by HOD)
        // Latest file is KEPT as history — never deleted
        [HttpPost]
        public async Task<IActionResult> Delete(int id)
        {
            var tt = await _context.Timetables.FindAsync(id);
            if (tt == null) return NotFound();

            // Latest file cannot be deleted — kept as history
            if (tt.IsLatest)
            {
                TempData["Error"] = "Cannot delete the latest timetable. Upload a new one to replace it.";
                return RedirectToAction(nameof(Index));
            }

            // Old files CAN be deleted by SuperAdmin
            if (User.IsInRole(AppRoles.SuperAdmin))
            {
                DeleteFile(tt.FilePath);
                tt.IsDeleted = true;
                await _context.SaveChangesAsync();
                TempData["Success"] = "Old timetable deleted.";
            }
            else
            {
                TempData["Error"] = "Only SuperAdmin can delete timetable history.";
            }

            return RedirectToAction(nameof(Index));
        }

        // ── HELPERS ───────────────────────────────────────
        private async Task<TimetableCreateVM> BuildVM(TimetableCreateVM vm)
        {
            vm.Departments = await _context.Departments
                .OrderBy(d => d.Name)
                .Select(d => new SelectListItem
                {
                    Value = d.DeptId.ToString(),
                    Text = d.Name
                }).ToListAsync();
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

        private void DeleteFile(string? filePath)
        {
            if (string.IsNullOrEmpty(filePath)) return;
            var fullPath = Path.Combine(_env.WebRootPath, filePath.TrimStart('/'));
            if (System.IO.File.Exists(fullPath)) System.IO.File.Delete(fullPath);
        }
    }
}