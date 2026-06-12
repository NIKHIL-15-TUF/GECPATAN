using GECPatan.Core.Data;
using  GECPatan.Core.Models.Domain;
using GECPatan.Admin.Models.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;

namespace GECPatan.Admin.Controllers
{
    [Authorize(Roles = "SuperAdmin,HOD,ContentEditor")]
    public class AcademicCalendarController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly IWebHostEnvironment _env;

        public AcademicCalendarController(ApplicationDbContext context, IWebHostEnvironment env)
        {
            _context = context;
            _env = env;
        }

        public async Task<IActionResult> Index(int? deptId)
        {
            ViewData["Title"] = "Academic Calendars";
            ViewBag.Departments = new SelectList(
                await _context.Departments.OrderBy(d => d.Name).ToListAsync(),
                "DeptId", "Name", deptId);
            ViewBag.SelectedDeptId = deptId;

            var query = _context.AcademicCalendars.AsQueryable();
            if (deptId.HasValue) query = query.Where(a => a.DeptId == deptId);

            var list = await query
                .OrderBy(a => a.DisplayOrder)
                .ThenByDescending(a => a.Id)
                .ToListAsync();

            var depts = await _context.Departments.ToDictionaryAsync(d => d.DeptId, d => d.Name);

            var vms = list.Select(a => new AcademicCalendarListVM
            {
                Id = a.Id,
                Title = a.Title,
                UploadDate = a.UploadDate,
                DeptName = a.DeptId.HasValue && depts.ContainsKey(a.DeptId.Value)
                             ? depts[a.DeptId.Value] : "All Departments",
                IsVisible = a.IsVisible,
                FilePath = a.FilePath
            }).ToList();

            return View(vms);
        }

        public async Task<IActionResult> Create()
        {
            ViewData["Title"] = "Add Academic Calendar";
            return View(await BuildVM(new AcademicCalendarVM
            {
                UploadDate = DateTime.Today.ToString("dd MMM yyyy")
            }));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(AcademicCalendarVM model, IFormFile? CalFile)
        {
            ViewData["Title"] = "Add Academic Calendar";
            if (!ModelState.IsValid)
                return View(await BuildVM(model));

            int maxOrder = await _context.AcademicCalendars
                .Select(a => (int?)a.DisplayOrder).MaxAsync() ?? -1;

            var ac = new AcademicCalendar
            {
                Title = model.Title,
                UploadDate = model.UploadDate,
                DeptId = model.DeptId,
                IsVisible = model.IsVisible,
                DisplayOrder = maxOrder + 1
            };

            if (CalFile != null && CalFile.Length > 0)
                ac.FilePath = await SaveFileAsync(CalFile, "academics");

            _context.AcademicCalendars.Add(ac);
            await _context.SaveChangesAsync();
            TempData["Success"] = "Academic Calendar added.";
            return RedirectToAction(nameof(Index));
        }

        public async Task<IActionResult> Edit(int id)
        {
            ViewData["Title"] = "Edit Academic Calendar";
            var a = await _context.AcademicCalendars.FindAsync(id);
            if (a == null) return NotFound();

            return View(await BuildVM(new AcademicCalendarVM
            {
                Id = a.Id,
                Title = a.Title,
                UploadDate = a.UploadDate,
                DeptId = a.DeptId,
                IsVisible = a.IsVisible,
                DisplayOrder = a.DisplayOrder,
                ExistingFilePath = a.FilePath
            }));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, AcademicCalendarVM model, IFormFile? CalFile)
        {
            ViewData["Title"] = "Edit Academic Calendar";
            if (!ModelState.IsValid)
                return View(await BuildVM(model));

            var a = await _context.AcademicCalendars.FindAsync(id);
            if (a == null) return NotFound();

            a.Title = model.Title;
            a.UploadDate = model.UploadDate;
            a.DeptId = model.DeptId;
            a.IsVisible = model.IsVisible;

            if (CalFile != null && CalFile.Length > 0)
            {
                DeleteFile(a.FilePath);
                a.FilePath = await SaveFileAsync(CalFile, "academics");
            }

            await _context.SaveChangesAsync();
            TempData["Success"] = "Academic Calendar updated.";
            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        public async Task<IActionResult> ToggleVisible(int id)
        {
            var a = await _context.AcademicCalendars.FindAsync(id);
            if (a == null) return NotFound();
            a.IsVisible = !a.IsVisible;
            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        public async Task<IActionResult> Delete(int id)
        {
            var a = await _context.AcademicCalendars.FindAsync(id);
            if (a == null) return NotFound();
            DeleteFile(a.FilePath);
            a.IsDeleted = true;
            await _context.SaveChangesAsync();
            TempData["Success"] = "Deleted.";
            return RedirectToAction(nameof(Index));
        }

        private async Task<AcademicCalendarVM> BuildVM(AcademicCalendarVM vm)
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