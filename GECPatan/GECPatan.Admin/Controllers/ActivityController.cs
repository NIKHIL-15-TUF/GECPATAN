using GECPatan.Core.Data;
using  GECPatan.Core.Models.Domain;
using GECPatan.Admin.Models.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;

namespace GECPatan.Admin.Controllers
{
    [Authorize(Roles = "SuperAdmin,HOD,ContentEditor,Principal")]
    public class ActivityController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly IWebHostEnvironment _env;

        public ActivityController(ApplicationDbContext context, IWebHostEnvironment env)
        {
            _context = context;
            _env = env;
        }

        // ── INDEX ─────────────────────────────────────────
        public async Task<IActionResult> Index(int? deptId, int? committeeId)
        {
            ViewData["Title"] = "Activities";
            ViewBag.SelectedDeptId = deptId;
            ViewBag.SelectedCommitteeId = committeeId;
            ViewBag.Departments = new SelectList(await _context.Departments.OrderBy(d => d.Name).ToListAsync(), "DeptId", "Name", deptId);
            ViewBag.Committees = new SelectList(await _context.CampusCommittees.OrderBy(c => c.Title).ToListAsync(), "Id", "Title", committeeId);

            var query = _context.Activities.AsQueryable();

            if (deptId.HasValue) query = query.Where(a => a.DeptId == deptId);
            if (committeeId.HasValue) query = query.Where(a => a.CommitteeId == committeeId);

            var list = await query
                .Include(a => a.Images)
                .Include(a => a.Files)
                .OrderByDescending(a => a.EventDate)
                .Select(a => new ActivityListVM
                {
                    Id = a.Id,
                    Title = a.Title,
                    EventDate = a.EventDate.HasValue ? a.EventDate.Value.ToString("dd MMM yyyy") : "",
                    IsVisible = a.IsVisible,
                    ImageCount = a.Images.Count,
                    FileCount = a.Files.Count
                })
                .ToListAsync();

            return View(list);
        }

        // ── CREATE GET ───
        public async Task<IActionResult> Create()
        {
            ViewData["Title"] = "Add Activity";
            return View(await BuildCreateVM(new ActivityCreateVM()));
        }

        // ── CREATE POST ──
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(ActivityCreateVM model)
        {
            ViewData["Title"] = "Add Activity";
            if (!ModelState.IsValid)
                return View(await BuildCreateVM(model));

            var activity = new Activity
            {
                Title = model.Title,
                Description = model.Description,
                EventDate = model.EventDate,
                EventTime = model.EventTime,
                Year = model.Year ?? model.EventDate?.Year,
                TargetStudents = model.TargetStudents,
                Keywords = model.Keywords,
                ExternalLink = model.ExternalLink,
                DeptId = model.DeptId,
                CommitteeId = model.CommitteeId,
                ClubId = model.ClubId,
                IsVisible = model.IsVisible,
                IsFile = model.IsFile
            };

            _context.Activities.Add(activity);
            await _context.SaveChangesAsync();

            // Save images
            int imgOrder = 0;
            foreach (var file in Request.Form.Files.Where(f => f.Name == "Images"))
            {
                if (file.Length > 0)
                {
                    _context.ActivityImages.Add(new ActivityImage
                    {
                        ActivityId = activity.Id,
                        ImagePath = await SaveFileAsync(file, "activities"),
                        DisplayOrder = imgOrder++
                    });
                }
            }

            // Save files
            int fileOrder = 0;
            foreach (var file in Request.Form.Files.Where(f => f.Name == "Files"))
            {
                if (file.Length > 0)
                {
                    _context.ActivityFiles.Add(new ActivityFile
                    {
                        ActivityId = activity.Id,
                        FilePath = await SaveFileAsync(file, "activities"),
                        Title = Path.GetFileNameWithoutExtension(file.FileName),
                        FileType = "PDF",
                        DisplayOrder = fileOrder++
                    });
                }
            }

            await _context.SaveChangesAsync();
            TempData["Success"] = "Activity added successfully.";
            return RedirectToAction(nameof(Index));
        }

        // ── EDIT GET ──────────────────────────────────────
        public async Task<IActionResult> Edit(int id)
        {
            ViewData["Title"] = "Edit Activity";

            var a = await _context.Activities
                .Include(x => x.Images)
                .Include(x => x.Files)
                .FirstOrDefaultAsync(x => x.Id == id);

            if (a == null) return NotFound();

            var vm = new ActivityEditVM
            {
                Id = a.Id,
                Title = a.Title,
                Description = a.Description,
                EventDate = a.EventDate,
                EventTime = a.EventTime,
                Year = a.Year,
                TargetStudents = a.TargetStudents,
                Keywords = a.Keywords,
                ExternalLink = a.ExternalLink,
                DeptId = a.DeptId,
                CommitteeId = a.CommitteeId,
                ClubId = a.ClubId,
                IsVisible = a.IsVisible,
                IsFile = a.IsFile
            };

            ViewBag.ExistingImages = a.Images.OrderBy(i => i.DisplayOrder).ToList();
            ViewBag.ExistingFiles = a.Files.OrderBy(f => f.DisplayOrder).ToList();

            return View(await BuildEditVM(vm));
        }

        // ── EDIT POST ─────────────────────────────────────
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, ActivityEditVM model)
        {
            ViewData["Title"] = "Edit Activity";
            if (!ModelState.IsValid)
                return View(await BuildEditVM(model));

            var a = await _context.Activities.FindAsync(id);
            if (a == null) return NotFound();

            a.Title = model.Title;
            a.Description = model.Description;
            a.EventDate = model.EventDate;
            a.EventTime = model.EventTime;
            a.Year = model.Year ?? model.EventDate?.Year;
            a.TargetStudents = model.TargetStudents;
            a.Keywords = model.Keywords;
            a.ExternalLink = model.ExternalLink;
            a.DeptId = model.DeptId;
            a.CommitteeId = model.CommitteeId;
            a.ClubId = model.ClubId;
            a.IsVisible = model.IsVisible;
            a.IsFile = model.IsFile;

            // Add new images
            int imgOrder = await _context.ActivityImages.Where(i => i.ActivityId == id).CountAsync();
            foreach (var file in Request.Form.Files.Where(f => f.Name == "Images"))
            {
                if (file.Length > 0)
                {
                    _context.ActivityImages.Add(new ActivityImage
                    {
                        ActivityId = id,
                        ImagePath = await SaveFileAsync(file, "activities"),
                        DisplayOrder = imgOrder++
                    });
                }
            }

            // Add new files
            int fileOrder = await _context.ActivityFiles.Where(f => f.ActivityId == id).CountAsync();
            foreach (var file in Request.Form.Files.Where(f => f.Name == "Files"))
            {
                if (file.Length > 0)
                {
                    _context.ActivityFiles.Add(new ActivityFile
                    {
                        ActivityId = id,
                        FilePath = await SaveFileAsync(file, "activities"),
                        Title = Path.GetFileNameWithoutExtension(file.FileName),
                        FileType = "PDF",
                        DisplayOrder = fileOrder++
                    });
                }
            }

            await _context.SaveChangesAsync();
            TempData["Success"] = "Activity updated.";
            return RedirectToAction(nameof(Index));
        }

        // ── TOGGLE VISIBLE ────────────────────────────────
        [HttpPost]
        public async Task<IActionResult> ToggleVisible(int id)
        {
            var a = await _context.Activities.FindAsync(id);
            if (a == null) return NotFound();
            a.IsVisible = !a.IsVisible;
            await _context.SaveChangesAsync();
            TempData["Success"] = "Visibility updated.";
            return RedirectToAction(nameof(Index));
        }

        // ── DELETE ────────────────────────────────────────
        [HttpPost]
        [Authorize(Roles = "SuperAdmin,HOD")]
        public async Task<IActionResult> Delete(int id)
        {
            var a = await _context.Activities.FindAsync(id);
            if (a == null) return NotFound();
            a.IsDeleted = true;
            await _context.SaveChangesAsync();
            TempData["Success"] = "Activity deleted.";
            return RedirectToAction(nameof(Index));
        }

        // ── DELETE IMAGE ──────────────────────────────────
        [HttpPost]
        public async Task<IActionResult> DeleteImage(int id, int activityId)
        {
            var img = await _context.ActivityImages.FindAsync(id);
            if (img != null) { DeleteFileFromDisk(img.ImagePath); img.IsDeleted = true; await _context.SaveChangesAsync(); }
            return RedirectToAction(nameof(Edit), new { id = activityId });
        }

        // ── DELETE FILE ───────────────────────────────────
        [HttpPost]
        public async Task<IActionResult> DeleteActivityFile(int id, int activityId)
        {
            var f = await _context.ActivityFiles.FindAsync(id);
            if (f != null) { DeleteFileFromDisk(f.FilePath); f.IsDeleted = true; await _context.SaveChangesAsync(); }
            return RedirectToAction(nameof(Edit), new { id = activityId });
        }

        // ── HELPERS ───────────────────────────────────────
        private async Task<ActivityCreateVM> BuildCreateVM(ActivityCreateVM vm)
        {
            vm.Departments = await _context.Departments.OrderBy(d => d.Name)
                .Select(d => new SelectListItem { Value = d.DeptId.ToString(), Text = d.Name }).ToListAsync();
            vm.Committees = await _context.CampusCommittees.OrderBy(c => c.Title)
                .Select(c => new SelectListItem { Value = c.Id.ToString(), Text = c.Title }).ToListAsync();
            return vm;
        }

        private async Task<ActivityEditVM> BuildEditVM(ActivityEditVM vm)
        {
            vm.Departments = await _context.Departments.OrderBy(d => d.Name)
                .Select(d => new SelectListItem { Value = d.DeptId.ToString(), Text = d.Name }).ToListAsync();
            vm.Committees = await _context.CampusCommittees.OrderBy(c => c.Title)
                .Select(c => new SelectListItem { Value = c.Id.ToString(), Text = c.Title }).ToListAsync();
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