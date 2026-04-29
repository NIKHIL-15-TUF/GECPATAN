using GECPatan.Admin.Data;
using GECPatan.Admin.Models.Domain;
using GECPatan.Admin.Models.ViewModels;
using GECPatan.Admin.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.CodeAnalysis.Elfie.Diagnostics;
using Microsoft.EntityFrameworkCore;
using NuGet.Protocol.Plugins;

namespace GECPatan.Admin.Controllers
{
    [Authorize(Roles = "SuperAdmin,Principal,HOD,Faculty")]
    public class FacultyController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly IWebHostEnvironment _env;
        private readonly NotificationService _notify;

        public FacultyController(ApplicationDbContext context, IWebHostEnvironment env, NotificationService notify)
        {
            _context = context;
            _env = env;
            _notify = notify;
        }

        // ── INDEX ─────────────────────────────────────────
        public async Task<IActionResult> Index(int? deptId)
        {
            ViewData["Title"] = "Faculty";
            ViewBag.Departments = new SelectList(
                await _context.Departments.OrderBy(d => d.DisplayOrder).ToListAsync(),
                "DeptId", "Name", deptId);
            ViewBag.SelectedDeptId = deptId;

            var query = _context.Faculties.Include(f => f.Department).AsQueryable();

            if (User.IsInRole(AppRoles.HOD))
            {
                var cu = await GetCurrentUserAsync();
                if (cu?.DeptId != null) query = query.Where(f => f.DeptId == cu.DeptId);
            }
            else if (User.IsInRole(AppRoles.Faculty) &&
                     !User.IsInRole(AppRoles.SuperAdmin) &&
                     !User.IsInRole(AppRoles.HOD))
            {
                var cu = await GetCurrentUserAsync();
                if (cu?.FacultyId != null) query = query.Where(f => f.FacultyId == cu.FacultyId);
            }
            else if (deptId.HasValue)
            {
                query = query.Where(f => f.DeptId == deptId.Value);
            }

            var list = await query
                .OrderBy(f => f.SeniorityOrder).ThenBy(f => f.Name)
                .Select(f => new FacultyListVM
                {
                    FacultyId = f.FacultyId,
                    Name = f.Name,
                    Designation = f.Designation,
                    ImagePath = f.ImagePath,
                    DepartmentName = f.Department != null ? f.Department.Name : "",
                    IsActive = f.IsActive,
                    IsTeaching = f.IsTeaching,
                    SeniorityOrder = f.SeniorityOrder,
                    DateOfJoining = f.DateOfJoining
                })
                .ToListAsync();

            return View(list);
        }

        // ── CREATE ────────────────────────────────────────
        [Authorize(Roles = "SuperAdmin,HOD")]
        public async Task<IActionResult> Create()
        {
            ViewData["Title"] = "Add Faculty";
            return View(await BuildCreateVM(new FacultyCreateVM()));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "SuperAdmin,HOD")]
        public async Task<IActionResult> Create(FacultyCreateVM model, IFormFile? Photo)
        {
            ViewData["Title"] = "Add Faculty";
            if (!ModelState.IsValid)
                return View(await BuildCreateVM(model));

            // Auto-set IsTeaching from designation
            bool isTeaching = AppRoles.TeachingDesignations.Contains(model.Designation);

            var faculty = new Faculty
            {
                Name = model.Name,
                Designation = model.Designation,
                DeptId = model.DeptId,
                DateOfJoining = model.DateOfJoining,
                AreaOfInterest = model.AreaOfInterest,
                Website = model.Website,
                IsTeaching = isTeaching,
                SeniorityOrder = model.SeniorityOrder,
                IsActive = true
            };
            var dept = await _context.Departments.FirstOrDefaultAsync(m =>m.DeptId == model.DeptId);

            if (Photo != null && Photo.Length > 0)
                faculty.ImagePath = await SaveFileAsync(Photo, "faculty");

            _context.Faculties.Add(faculty);
            await _context.SaveChangesAsync();
            await _notify.SendAsync(
                title: $"New Faculty Added: {faculty.Name}",
                message: $"Added to {dept?.Name??"Unknown"} department",
                module: "Faculty",
                icon: "fa-user-plus",
                color: "success",
                link: $"/Faculty/Edit/{faculty.FacultyId}",
                forRole: "SuperAdmin"
            );
            TempData["Success"] = $"Faculty '{faculty.Name}' added.";
            return RedirectToAction(nameof(Index));
        }

        // ── EDIT ──────────────────────────────────────────
        public async Task<IActionResult> Edit(int id)
        {
            ViewData["Title"] = "Edit Faculty";
            var f = await _context.Faculties.Include(x => x.Department)
                .FirstOrDefaultAsync(x => x.FacultyId == id);
            if (f == null) return NotFound();

            // Role restrictions
            if (User.IsInRole(AppRoles.Faculty) &&
                !User.IsInRole(AppRoles.SuperAdmin) &&
                !User.IsInRole(AppRoles.HOD))
            {
                var cu = await GetCurrentUserAsync();
                if (cu?.FacultyId != id) return Forbid();
            };
            var vm = new FacultyEditVM
            {
                FacultyId = f.FacultyId,
                Name = f.Name,
                Designation = f.Designation,
                DeptId = f.DeptId,
                DateOfJoining = f.DateOfJoining,
                AreaOfInterest = f.AreaOfInterest,
                Website = f.Website,
                IsTeaching = f.IsTeaching,
                SeniorityOrder = f.SeniorityOrder,
                ExistingImagePath = f.ImagePath
            };
            return View(await BuildEditVM(vm));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, FacultyEditVM model, IFormFile? Photo)
        {
            ViewData["Title"] = "Edit Faculty";
            if (!ModelState.IsValid)
                return View(await BuildEditVM(model));

            var f = await _context.Faculties.FindAsync(id);
            if (f == null) return NotFound();

            // Auto-set IsTeaching
            bool isTeaching = AppRoles.TeachingDesignations.Contains(model.Designation);

            f.Name = model.Name;
            f.Designation = model.Designation;
            f.DeptId = model.DeptId;
            f.DateOfJoining = model.DateOfJoining;
            f.AreaOfInterest = model.AreaOfInterest;
            f.Website = model.Website;
            f.IsTeaching = isTeaching;
            f.SeniorityOrder = model.SeniorityOrder;

            if (Photo != null && Photo.Length > 0)
            {
                DeleteFile(f.ImagePath);
                f.ImagePath = await SaveFileAsync(Photo, "faculty");
            }

            await _context.SaveChangesAsync();
            TempData["Success"] = $"Faculty '{f.Name}' updated.";
            await _notify.SendAsync(
                title: $"Faculty Updated: {f.Name}",
                message: "Profile details were changed",
                module: "Faculty",
                icon: "fa-edit",
                color: "info",
                link: $"/Faculty/Edit/{f.FacultyId}",
                forRole: "SuperAdmin"
            );
            return RedirectToAction(nameof(Index));
        }

        // ── TOGGLE / DELETE / REORDER ─────────────────────
        [HttpPost]
        [Authorize(Roles = "SuperAdmin,HOD")]
        public async Task<IActionResult> ToggleActive(int id)
        {
            var f = await _context.Faculties.FindAsync(id);
            if (f == null) return NotFound();
            f.IsActive = !f.IsActive;
            await _context.SaveChangesAsync();
            TempData["Success"] = $"'{f.Name}' " + (f.IsActive ? "activated" : "deactivated") + ".";
            await _notify.SendAsync(
                title: $"Faculty {(f.IsActive ? "Activated" : "Deactivated")}: {f.Name}",
                message: null,
                module: "Faculty",
                icon: f.IsActive ? "fa-user-check" : "fa-user-slash",
                color: f.IsActive ? "success" : "warning",
                link: $"/Faculty/Edit/{f.FacultyId}",
                forRole: "SuperAdmin"
            );
            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        [Authorize(Roles = "SuperAdmin")]
        public async Task<IActionResult> Delete(int id)
        {
            var f = await _context.Faculties.FindAsync(id);
            if (f == null) return NotFound();
            f.IsDeleted = true; f.IsActive = false;
            await _context.SaveChangesAsync();
            TempData["Success"] = $"Faculty '{f.Name}' deleted.";
            await _notify.SendAsync(
                title: $"Faculty Deleted: {f.Name}",
                message: null,
                module: "Faculty",
                icon: "fa-user-times",
                color: "danger",
                link: "/Faculty/Index",
                forRole: "SuperAdmin"
            );
            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        [Authorize(Roles = "SuperAdmin,HOD")]
        public async Task<IActionResult> Reorder(int id, string direction)
        {
            var f = await _context.Faculties.FindAsync(id);
            if (f == null) return NotFound();
            if (direction == "up")
            {
                var above = await _context.Faculties
                    .Where(x => x.DeptId == f.DeptId && x.SeniorityOrder == f.SeniorityOrder - 1)
                    .FirstOrDefaultAsync();
                if (above != null) { above.SeniorityOrder++; f.SeniorityOrder--; }
            }
            else
            {
                var below = await _context.Faculties
                    .Where(x => x.DeptId == f.DeptId && x.SeniorityOrder == f.SeniorityOrder + 1)
                    .FirstOrDefaultAsync();
                if (below != null) { below.SeniorityOrder--; f.SeniorityOrder++; }
            }
            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        // ══════════════════════════════════════════════════
        // SUB SECTIONS
        // ══════════════════════════════════════════════════

        // ── QUALIFICATIONS ────────────────────────────────
        public async Task<IActionResult> Qualifications(int id)
        {
            ViewData["Title"] = "Qualifications";
            ViewBag.FacultyId = id;
            ViewBag.FacultyName = (await _context.Faculties.FindAsync(id))?.Name;
            var list = await _context.FacultyQualifications
                .Where(q => q.FacultyId == id).ToListAsync();
            return View(list);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AddQualification(QualificationVM model)
        {
            if (ModelState.IsValid)
            {
                _context.FacultyQualifications.Add(new FacultyQualification
                {
                    FacultyId = model.FacultyId,
                    Degree = model.Degree,
                    University = model.University,
                    Year = model.Year,
                    Specialization = model.Specialization
                });
                await _context.SaveChangesAsync();
                TempData["Success"] = "Qualification added.";
                var faculty = await _context.Faculties.FirstOrDefaultAsync(f => f.FacultyId == model.FacultyId);
                await _notify.SendAsync(
                   title: $"Qualification Added for {faculty?.Name}",
                   message: $"{model.Degree} - {model.Specialization}",
                   module: "Faculty",
                   icon: "fa-graduation-cap",
                   color: "info",
                   link: $"/Faculty/Edit/{model.FacultyId}",
                   forRole: "SuperAdmin"
               );
            }
            return RedirectToAction(nameof(Qualifications), new { id = model.FacultyId });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteQualification(int id, int facultyId)
        {
            var item = await _context.FacultyQualifications.FindAsync(id);
            if (item != null) { item.IsDeleted = true; await _context.SaveChangesAsync(); }
            var facultyName = await _context.Faculties
             .Where(f => f.FacultyId == facultyId)
             .Select(f => f.Name)
             .FirstOrDefaultAsync();
            var degree = item.Degree;
            var specialization = item.Specialization;
            await _notify.SendAsync(
                title: $"Qualification Removed for {facultyName}",
                message: $"{degree} - {specialization}",
                module: "Faculty",
                icon: "fa-user-minus",
                color: "warning",
                link: $"/Faculty/Edit/{facultyId}",
                forRole: "SuperAdmin"
            );
            return RedirectToAction(nameof(Qualifications), new { id = facultyId });
        }

        // ── EXPERIENCE ────────────────────────────────────
        public async Task<IActionResult> Experience(int id)
        {
            ViewData["Title"] = "Experience";
            ViewBag.FacultyId = id;
            ViewBag.FacultyName = (await _context.Faculties.FindAsync(id))?.Name;
            var list = await _context.FacultyExperiences
                .Where(e => e.FacultyId == id).ToListAsync();
            return View(list);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AddExperience(ExperienceVM model)
        {
            if (ModelState.IsValid)
            {
                _context.FacultyExperiences.Add(new FacultyExperience
                {
                    FacultyId = model.FacultyId,
                    Position = model.Position,
                    Organization = model.Organization,
                    FromDate = model.FromDate,
                    ToDate = model.ToDate
                });
                var facultyName = await _context.Faculties
                    .Where(f => f.FacultyId == model.FacultyId)
                    .Select(f => f.Name)
                    .FirstOrDefaultAsync();
                await _context.SaveChangesAsync();
                await _notify.SendAsync(
                    title: $"Experience Added for {facultyName}",
                    message: $"{model.Position} at {model.Organization}",
                    module: "Faculty",
                    icon: "fa-briefcase",
                    color: "info",
                    link: $"/Faculty/Edit/{model.FacultyId}",
                    forRole: "SuperAdmin"
                );

                TempData["Success"] = "Experience added.";
            }
            return RedirectToAction(nameof(Experience), new { id = model.FacultyId });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteExperience(int id, int facultyId)
        {
            var item = await _context.FacultyExperiences.FindAsync(id);
            if (item != null) {
                var facultyName = await _context.Faculties
                    .Where(f => f.FacultyId == facultyId)
                    .Select(f => f.Name)
                    .FirstOrDefaultAsync();

                var position = item.Position;
                var org = item.Organization;
                await _notify.SendAsync(
                    title: $"Experience Removed for {facultyName}",
                    message: $"{position} at {org}",
                    module: "Faculty",
                    icon: "fa-user-minus",
                    color: "warning",
                    link: $"/Faculty/Edit/{facultyId}",
                    forRole: "SuperAdmin"
                );
                item.IsDeleted = true; await _context.SaveChangesAsync(); 
            }
            return RedirectToAction(nameof(Experience), new { id = facultyId });
        }

        // ── TRAINING ──────────────────────────────────────
        public async Task<IActionResult> Training(int id)
        {
            ViewData["Title"] = "Training";
            ViewBag.FacultyId = id;
            ViewBag.FacultyName = (await _context.Faculties.FindAsync(id))?.Name;
            var list = await _context.FacultyTrainings
                .Where(t => t.FacultyId == id && !t.IsDeleted).ToListAsync();
            return View(list);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AddTraining(TrainingVM model)
        {
            if (ModelState.IsValid)
            {
                _context.FacultyTrainings.Add(new FacultyTraining
                {
                    FacultyId = model.FacultyId,
                    Title = model.Title,
                    OrganizedBy = model.OrganizedBy,
                    FromDate = model.FromDate,
                    ToDate = model.ToDate
                });
                await _context.SaveChangesAsync();

                var facultyName = await _context.Faculties
                    .Where(f => f.FacultyId == model.FacultyId)
                    .Select(f => f.Name)
                    .FirstOrDefaultAsync();

                await _notify.SendAsync(
                    title: $"Training Added for {facultyName}",
                    message: $"{model.Title} by {model.OrganizedBy}",
                    module: "Faculty",
                    icon: "fa-chalkboard-teacher",
                    color: "info",
                    link: $"/Faculty/Edit/{model.FacultyId}",
                    forRole: "SuperAdmin"
                );
                TempData["Success"] = "Training added.";
            }
            return RedirectToAction(nameof(Training), new { id = model.FacultyId });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteTraining(int id, int facultyId)
        {
            var item = await _context.FacultyTrainings.FindAsync(id);
            if (item != null) {
                item.IsDeleted = true;
                await _context.SaveChangesAsync();
                var title=item.Title;
                var org = item.OrganizedBy;
                var facultyName = await _context.Faculties
                .Where(f => f.FacultyId == facultyId)
                .Select(f => f.Name)
                .FirstOrDefaultAsync();

                        await _notify.SendAsync(
                            title: $"Training Removed for {facultyName}",
                            message: $"{title} by {org}",
                            module: "Faculty",
                            icon: "fa-user-minus",
                            color: "warning",
                            link: $"/Faculty/Edit/{facultyId}",
                            forRole: "SuperAdmin"
                        );
            }
            return RedirectToAction(nameof(Training), new { id = facultyId });
        }

        // ── PUBLICATIONS ──────────────────────────────────
        public async Task<IActionResult> Publications(int id)
        {
            ViewData["Title"] = "Publications";
            ViewBag.FacultyId = id;
            ViewBag.FacultyName = (await _context.Faculties.FindAsync(id))?.Name;
            var list = await _context.FacultyPublications
                .Where(p => p.FacultyId == id && !p.IsDeleted).OrderBy(p => p.SrNo).ToListAsync();

            return View(list);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AddPublication(PublicationVM model)
        {
            if (ModelState.IsValid)
            {
                _context.FacultyPublications.Add(new FacultyPublication
                {
                    FacultyId = model.FacultyId,
                    SrNo = model.SrNo,
                    Title = model.Title
                });
                var facultyName = await _context.Faculties
                  .Where(f => f.FacultyId == model.FacultyId)
                  .Select(f => f.Name)
                  .FirstOrDefaultAsync();
                await _context.SaveChangesAsync();
                await _notify.SendAsync(
                   title: $"Publication Added for {facultyName}",
                   message: $"[{model.SrNo}] {model.Title}",
                   module: "Faculty",
                   icon: "fa-book",
                   color: "info",
                   link: $"/Faculty/Edit/{model.FacultyId}",
                   forRole: "SuperAdmin"
               );
                TempData["Success"] = "Publication added.";
            }
            return RedirectToAction(nameof(Publications), new { id = model.FacultyId });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeletePublication(int id, int facultyId)
        {
            var item = await _context.FacultyPublications.FindAsync(id);
            if (item != null) {
                var title = item.Title;
                var srNo = item.SrNo;
                item.IsDeleted = true; await _context.SaveChangesAsync();
                var facultyName = await _context.Faculties
                .Where(f => f.FacultyId == facultyId)
                .Select(f => f.Name)
                .FirstOrDefaultAsync();

                await _notify.SendAsync(
                    title: $"Publication Removed for {facultyName}",
                    message: $"[{srNo}] {title}",
                    module: "Faculty",
                    icon: "fa-trash",
                    color: "warning",
                    link: $"/Faculty/Edit/{facultyId}",
                    forRole: "SuperAdmin"
                );
            }
            return RedirectToAction(nameof(Publications), new { id = facultyId });
        }

        // ── PERSONAL DETAILS ──
        public async Task<IActionResult> PersonalDetails(int id)
        {
            ViewData["Title"] = "Personal Details";
            ViewBag.FacultyId = id;
            ViewBag.FacultyName = (await _context.Faculties.FindAsync(id))?.Name;
            var detail = await _context.PersonalDetails.FirstOrDefaultAsync(p => p.FacultyId == id);
            return View(new PersonalDetailVM
            {
                FacultyId = id,
                PersonalDetailId = detail?.PersonalDetailId ?? 0,
                Department = detail?.Department,
                Contact = detail?.Contact,
                Email = detail?.Email
            });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SavePersonalDetails(PersonalDetailVM model)
        {
            var existing = await _context.PersonalDetails
                .FirstOrDefaultAsync(p => p.FacultyId == model.FacultyId);
            if (existing == null)
                _context.PersonalDetails.Add(new PersonalDetail
                {
                    FacultyId = model.FacultyId,
                    Department = model.Department,
                    Contact = model.Contact,
                    Email = model.Email
                });
            else
            {
                existing.Department = model.Department;
                existing.Contact = model.Contact;
                existing.Email = model.Email;
            }
            await _context.SaveChangesAsync();
            var facultyName = await _context.Faculties
               .Where(f => f.FacultyId == model.FacultyId)
               .Select(f => f.Name)
               .FirstOrDefaultAsync();

            await _notify.SendAsync(
                title: $"Personal Details Updated",
                message: $"{facultyName}'s profile updated",
                module: "Faculty",
                icon: "fa-id-card",
                color: "primary",
                link: $"/Faculty/Edit/{model.FacultyId}",
                forRole: "SuperAdmin"
            );
            TempData["Success"] = "Personal details saved.";
            return RedirectToAction(nameof(PersonalDetails), new { id = model.FacultyId });
        }

        // ── HELPERS ───────────────────────────────────────
        private async Task<FacultyCreateVM> BuildCreateVM(FacultyCreateVM vm)
        {
            vm.Departments = await _context.Departments.OrderBy(d => d.DisplayOrder)
                .Select(d => new SelectListItem
                {
                    Value = d.DeptId.ToString(),
                    Text = d.Name
                }).ToListAsync();

            vm.Designations = AppRoles.AllDesignations
                .Select(d => new SelectListItem { Value = d, Text = d })
                .ToList();

            return vm;
        }

        private async Task<FacultyEditVM> BuildEditVM(FacultyEditVM vm)
        {
            vm.Departments = await _context.Departments.OrderBy(d => d.DisplayOrder)
                .Select(d => new SelectListItem
                {
                    Value = d.DeptId.ToString(),
                    Text = d.Name
                }).ToListAsync();

            vm.Designations = AppRoles.AllDesignations
                .Select(d => new SelectListItem { Value = d, Text = d })
                .ToList();

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

        private async Task<ApplicationUser?> GetCurrentUserAsync()
        {
            var userId = User.FindFirst(
                System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
            if (userId == null) return null;
            return await _context.Users.FindAsync(userId);
        }
    }
}