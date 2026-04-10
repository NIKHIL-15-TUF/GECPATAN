using GECPatan.Admin.Data;
using GECPatan.Admin.Models.Domain;
using GECPatan.Admin.Models.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;

namespace GECPatan.Admin.Controllers
{
    [Authorize(Roles = "SuperAdmin,Principal,HOD,Faculty")]
    public class FacultyController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly IWebHostEnvironment _env;

        public FacultyController(
            ApplicationDbContext context,
            IWebHostEnvironment env)
        {
            _context = context;
            _env = env;
        }

        // ── INDEX ──
        public async Task<IActionResult> Index(int? deptId)
        {
            ViewData["Title"] = "Faculty";

            var departments = await _context.Departments
                .OrderBy(d => d.DisplayOrder)
                .ToListAsync();

            ViewBag.Departments = new SelectList(departments, "DeptId", "Name", deptId);
            ViewBag.SelectedDeptId = deptId;

            var query = _context.Faculties
                .Include(f => f.Department)
                .AsQueryable();

            // HOD sees only their dept
            if (User.IsInRole(AppRoles.HOD))
            {
                var currentUser = await GetCurrentUserAsync();
                if (currentUser?.DeptId != null)
                    query = query.Where(f => f.DeptId == currentUser.DeptId);
            }
            // Faculty sees only themselves
            else if (User.IsInRole(AppRoles.Faculty) &&
                     !User.IsInRole(AppRoles.SuperAdmin) &&
                     !User.IsInRole(AppRoles.HOD))
            {
                var currentUser = await GetCurrentUserAsync();
                if (currentUser?.FacultyId != null)
                    query = query.Where(f => f.FacultyId == currentUser.FacultyId);
            }
            // Filter by dept if selected
            else if (deptId.HasValue)
            {
                query = query.Where(f => f.DeptId == deptId.Value);
            }

            var faculties = await query
                .OrderBy(f => f.SeniorityOrder)
                .ThenBy(f => f.Name)
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

            return View(faculties);
        }

        // ── CREATE GET ──
        [Authorize(Roles = "SuperAdmin,HOD")]
        public async Task<IActionResult> Create()
        {
            ViewData["Title"] = "Add Faculty";
            var vm = new FacultyCreateVM
            {
                Departments = await GetDepartmentList()
            };
            return View(vm);
        }

        // ── CREATE POST ───
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "SuperAdmin,HOD")]
        public async Task<IActionResult> Create(
            FacultyCreateVM model,
            IFormFile? Photo)
        {
            ViewData["Title"] = "Add Faculty";
            model.Departments = await GetDepartmentList();

            if (!ModelState.IsValid)
                return View(model);

            // HOD can only add to their dept
            if (User.IsInRole(AppRoles.HOD))
            {
                var currentUser = await GetCurrentUserAsync();
                if (currentUser?.DeptId != model.DeptId)
                {
                    ModelState.AddModelError("", "You can only add faculty to your department.");
                    return View(model);
                }
            }

            var faculty = new Faculty
            {
                Name = model.Name,
                Designation = model.Designation,
                DeptId = model.DeptId,
                DateOfJoining = model.DateOfJoining,
                Qualification = model.Qualification,
                AreaOfInterest = model.AreaOfInterest,
                Website = model.Website,
                IsTeaching = model.IsTeaching,
                SeniorityOrder = model.SeniorityOrder,
                IsActive = true
            };

            if (Photo != null && Photo.Length > 0)
                faculty.ImagePath = await SaveFileAsync(Photo, "faculty");

            _context.Faculties.Add(faculty);
            await _context.SaveChangesAsync();

            TempData["Success"] = $"Faculty '{faculty.Name}' added successfully.";
            return RedirectToAction(nameof(Index));
        }

        // ── EDIT GET ───
        public async Task<IActionResult> Edit(int id)
        {
            ViewData["Title"] = "Edit Faculty";

            var faculty = await _context.Faculties
                .Include(f => f.Department)
                .FirstOrDefaultAsync(f => f.FacultyId == id);

            if (faculty == null)
                return NotFound();

            // Faculty can only edit themselves
            if (User.IsInRole(AppRoles.Faculty) &&
                !User.IsInRole(AppRoles.SuperAdmin) &&
                !User.IsInRole(AppRoles.HOD))
            {
                var currentUser = await GetCurrentUserAsync();
                if (currentUser?.FacultyId != id)
                    return Forbid();
            }

            // HOD can only edit their dept
            if (User.IsInRole(AppRoles.HOD))
            {
                var currentUser = await GetCurrentUserAsync();
                if (currentUser?.DeptId != faculty.DeptId)
                    return Forbid();
            }

            var vm = new FacultyEditVM
            {
                FacultyId = faculty.FacultyId,
                Name = faculty.Name,
                Designation = faculty.Designation,
                DeptId = faculty.DeptId,
                DateOfJoining = faculty.DateOfJoining,
                Qualification = faculty.Qualification,
                AreaOfInterest = faculty.AreaOfInterest,
                Website = faculty.Website,
                IsTeaching = faculty.IsTeaching,
                SeniorityOrder = faculty.SeniorityOrder,
                ExistingImagePath = faculty.ImagePath,
                Departments = await GetDepartmentList()
            };

            return View(vm);
        }

        // ── EDIT POST ──
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(
            int id,
            FacultyEditVM model,
            IFormFile? Photo)
        {
            ViewData["Title"] = "Edit Faculty";
            model.Departments = await GetDepartmentList();

            if (id != model.FacultyId)
                return NotFound();

            if (!ModelState.IsValid)
                return View(model);

            var faculty = await _context.Faculties.FindAsync(id);
            if (faculty == null)
                return NotFound();

            // Role restrictions
            if (User.IsInRole(AppRoles.Faculty) &&
                !User.IsInRole(AppRoles.SuperAdmin) &&
                !User.IsInRole(AppRoles.HOD))
            {
                var currentUser = await GetCurrentUserAsync();
                if (currentUser?.FacultyId != id)
                    return Forbid();
            }

            faculty.Name = model.Name;
            faculty.Designation = model.Designation;
            faculty.DeptId = model.DeptId;
            faculty.DateOfJoining = model.DateOfJoining;
            faculty.Qualification = model.Qualification;
            faculty.AreaOfInterest = model.AreaOfInterest;
            faculty.Website = model.Website;
            faculty.IsTeaching = model.IsTeaching;
            faculty.SeniorityOrder = model.SeniorityOrder;

            if (Photo != null && Photo.Length > 0)
            {
                DeleteFile(faculty.ImagePath);
                faculty.ImagePath = await SaveFileAsync(Photo, "faculty");
            }

            await _context.SaveChangesAsync();

            TempData["Success"] = $"Faculty '{faculty.Name}' updated successfully.";
            return RedirectToAction(nameof(Index));
        }

        // ── TOGGLE ACTIVE ──
        [HttpPost]
        [Authorize(Roles = "SuperAdmin,HOD")]
        public async Task<IActionResult> ToggleActive(int id)
        {
            var faculty = await _context.Faculties.FindAsync(id);
            if (faculty == null) return NotFound();

            faculty.IsActive = !faculty.IsActive;
            await _context.SaveChangesAsync();

            TempData["Success"] = $"'{faculty.Name}' " +
                (faculty.IsActive ? "activated" : "deactivated") + ".";
            return RedirectToAction(nameof(Index));
        }

        // ── DELETE ───
        [HttpPost]
        [Authorize(Roles = "SuperAdmin")]
        public async Task<IActionResult> Delete(int id)
        {
            var faculty = await _context.Faculties.FindAsync(id);
            if (faculty == null) return NotFound();

            faculty.IsDeleted = true;
            faculty.IsActive = false;
            await _context.SaveChangesAsync();

            TempData["Success"] = $"Faculty '{faculty.Name}' deleted.";
            return RedirectToAction(nameof(Index));
        }

        // ── REORDER ──
        [HttpPost]
        [Authorize(Roles = "SuperAdmin,HOD")]
        public async Task<IActionResult> Reorder(int id, string direction)
        {
            var faculty = await _context.Faculties.FindAsync(id);
            if (faculty == null) return NotFound();

            if (direction == "up" && faculty.SeniorityOrder > 0)
            {
                var above = await _context.Faculties
                    .Where(f => f.DeptId == faculty.DeptId &&
                                f.SeniorityOrder == faculty.SeniorityOrder - 1)
                    .FirstOrDefaultAsync();
                if (above != null)
                {
                    above.SeniorityOrder++;
                    faculty.SeniorityOrder--;
                }
            }
            else if (direction == "down")
            {
                var below = await _context.Faculties
                    .Where(f => f.DeptId == faculty.DeptId &&
                                f.SeniorityOrder == faculty.SeniorityOrder + 1)
                    .FirstOrDefaultAsync();
                if (below != null)
                {
                    below.SeniorityOrder--;
                    faculty.SeniorityOrder++;
                }
            }

            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        // ════════════
        // SUB SECTIONS
        // ════════════

        // ── QUALIFICATIONS ─-
        public async Task<IActionResult> Qualifications(int id)
        {
            ViewData["Title"] = "Qualifications";
            ViewBag.FacultyId = id;
            ViewBag.FacultyName = (await _context.Faculties.FindAsync(id))?.Name;

            var list = await _context.FacultyQualifications
                .Where(q => q.FacultyId == id)
                .ToListAsync();

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
            }
            return RedirectToAction(nameof(Qualifications), new { id = model.FacultyId });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteQualification(int id, int facultyId)
        {
            var item = await _context.FacultyQualifications.FindAsync(id);
            if (item != null)
            {
                item.IsDeleted = true;
                await _context.SaveChangesAsync();
                TempData["Success"] = "Qualification removed.";
            }
            return RedirectToAction(nameof(Qualifications), new { id = facultyId });
        }

        // ── EXPERIENCE ──
        public async Task<IActionResult> Experience(int id)
        {
            ViewData["Title"] = "Professional Experience";
            ViewBag.FacultyId = id;
            ViewBag.FacultyName = (await _context.Faculties.FindAsync(id))?.Name;

            var list = await _context.FacultyExperiences
                .Where(e => e.FacultyId == id)
                .ToListAsync();

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
                    Duration = model.Duration
                });
                await _context.SaveChangesAsync();
                TempData["Success"] = "Experience added.";
            }
            return RedirectToAction(nameof(Experience), new { id = model.FacultyId });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteExperience(int id, int facultyId)
        {
            var item = await _context.FacultyExperiences.FindAsync(id);
            if (item != null)
            {
                item.IsDeleted = true;
                await _context.SaveChangesAsync();
                TempData["Success"] = "Experience removed.";
            }
            return RedirectToAction(nameof(Experience), new { id = facultyId });
        }

        // ── TRAINING ───
        public async Task<IActionResult> Training(int id)
        {
            ViewData["Title"] = "Training & Workshops";
            ViewBag.FacultyId = id;
            ViewBag.FacultyName = (await _context.Faculties.FindAsync(id))?.Name;

            var list = await _context.FacultyTrainings
                .Where(t => t.FacultyId == id)
                .ToListAsync();

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
                    Date = model.Date
                });
                await _context.SaveChangesAsync();
                TempData["Success"] = "Training added.";
            }
            return RedirectToAction(nameof(Training), new { id = model.FacultyId });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteTraining(int id, int facultyId)
        {
            var item = await _context.FacultyTrainings.FindAsync(id);
            if (item != null)
            {
                item.IsDeleted = true;
                await _context.SaveChangesAsync();
                TempData["Success"] = "Training removed.";
            }
            return RedirectToAction(nameof(Training), new { id = facultyId });
        }

        // ── PUBLICATIONS ───
        public async Task<IActionResult> Publications(int id)
        {
            ViewData["Title"] = "Publications";
            ViewBag.FacultyId = id;
            ViewBag.FacultyName = (await _context.Faculties.FindAsync(id))?.Name;

            var list = await _context.FacultyPublications
                .Where(p => p.FacultyId == id)
                .OrderBy(p => p.SrNo)
                .ToListAsync();

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
                await _context.SaveChangesAsync();
                TempData["Success"] = "Publication added.";
            }
            return RedirectToAction(nameof(Publications), new { id = model.FacultyId });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeletePublication(int id, int facultyId)
        {
            var item = await _context.FacultyPublications.FindAsync(id);
            if (item != null)
            {
                item.IsDeleted = true;
                await _context.SaveChangesAsync();
                TempData["Success"] = "Publication removed.";
            }
            return RedirectToAction(nameof(Publications), new { id = facultyId });
        }

        // ── PERSONAL DETAILS ──
        public async Task<IActionResult> PersonalDetails(int id)
        {
            ViewData["Title"] = "Personal Details";
            ViewBag.FacultyId = id;
            ViewBag.FacultyName = (await _context.Faculties.FindAsync(id))?.Name;

            var detail = await _context.PersonalDetails
                .FirstOrDefaultAsync(p => p.FacultyId == id);

            var vm = new PersonalDetailVM
            {
                FacultyId = id,
                PersonalDetailId = detail?.PersonalDetailId ?? 0,
                Department = detail?.Department,
                Contact = detail?.Contact,
                Email = detail?.Email
            };

            return View(vm);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SavePersonalDetails(PersonalDetailVM model)
        {
            var existing = await _context.PersonalDetails
                .FirstOrDefaultAsync(p => p.FacultyId == model.FacultyId);

            if (existing == null)
            {
                _context.PersonalDetails.Add(new PersonalDetail
                {
                    FacultyId = model.FacultyId,
                    Department = model.Department,
                    Contact = model.Contact,
                    Email = model.Email
                });
            }
            else
            {
                existing.Department = model.Department;
                existing.Contact = model.Contact;
                existing.Email = model.Email;
            }

            await _context.SaveChangesAsync();
            TempData["Success"] = "Personal details saved.";
            return RedirectToAction(nameof(PersonalDetails), new { id = model.FacultyId });
        }

        // ── HELPERS ──
        private async Task<List<SelectListItem>> GetDepartmentList()
        {
            return await _context.Departments
                .OrderBy(d => d.DisplayOrder)
                .Select(d => new SelectListItem
                {
                    Value = d.DeptId.ToString(),
                    Text = d.Name
                })
                .ToListAsync();
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
            if (System.IO.File.Exists(fullPath))
                System.IO.File.Delete(fullPath);
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