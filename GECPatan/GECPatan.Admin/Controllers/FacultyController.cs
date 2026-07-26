using GECPatan.Core.Data;
using GECPatan.Core.Models.Domain;
using GECPatan.Admin.Models.ViewModels;
using GECPatan.Core.Services;
using GECPatan.Core.Services.FileStorage;
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
        private readonly NotificationService _notify;
        private readonly IFileStorageService _fileStorage;
        private readonly ILogger<FacultyController> _logger;

        public FacultyController(
            ApplicationDbContext context,
            NotificationService notify,
            IFileStorageService fileStorage,
            ILogger<FacultyController> logger)
        {
            _context = context;
            _notify = notify;
            _fileStorage = fileStorage;
            _logger = logger;
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

            string? savedImagePath = null;
            if (Photo != null && Photo.Length > 0)
            {
                var uploadResult = await _fileStorage.SaveAsync(Photo, "faculty", FileCategory.Image);
                if (!uploadResult.Success)
                {
                    ModelState.AddModelError(nameof(Photo), uploadResult.ErrorMessage!);
                    return View(await BuildCreateVM(model));
                }
                savedImagePath = uploadResult.RelativePath;
            }

            var faculty = new Faculty
            {
                Name = model.Name,
                Designation = model.Designation,
                DeptId = model.DeptId,
                DateOfJoining = model.DateOfJoining,
                AreaOfInterest = model.AreaOfInterest,
                LetterNumber = model.LetterNumber,
                Website = model.Website,
                IsTeaching = isTeaching,
                SeniorityOrder = model.SeniorityOrder,
                IsActive = true,
                ImagePath = savedImagePath
            };
            var dept = await _context.Departments.FirstOrDefaultAsync(m => m.DeptId == model.DeptId);

            _context.Faculties.Add(faculty);
            await _context.SaveChangesAsync();

            _logger.LogInformation("Faculty {FacultyId} '{Name}' created by {User}",
                faculty.FacultyId, faculty.Name, User.Identity?.Name);

            await TryNotifyAsync(
                title: $"New Faculty Added: {faculty.Name}",
                message: $"Added to {dept?.Name ?? "Unknown"} department",
                icon: "fa-user-plus",
                color: "success",
                link: $"/Faculty/Edit/{faculty.FacultyId}");

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
            }
            ;
            var vm = new FacultyEditVM
            {
                FacultyId = f.FacultyId,
                Name = f.Name,
                Designation = f.Designation,
                DeptId = f.DeptId,
                DateOfJoining = f.DateOfJoining,
                AreaOfInterest = f.AreaOfInterest,
                LetterNumber = f.LetterNumber,
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

            // Was completely missing before — the GET action checked this,
            // but this POST (the actual mutation) did not, meaning any
            // authenticated Faculty user could edit ANY other faculty
            // member's profile by submitting a form with a different id.
            if (!await CanManageFacultyAsync(id))
            {
                _logger.LogWarning(
                    "User {User} was denied edit access to faculty {FacultyId} (not their own profile / department)",
                    User.Identity?.Name, id);
                return Forbid();
            }

            // Auto-set IsTeaching
            bool isTeaching = AppRoles.TeachingDesignations.Contains(model.Designation);

            f.Name = model.Name;
            f.Designation = model.Designation;
            f.DeptId = model.DeptId;
            f.DateOfJoining = model.DateOfJoining;
            f.AreaOfInterest = model.AreaOfInterest;
            f.LetterNumber = model.LetterNumber;
            f.Website = model.Website;
            f.IsTeaching = isTeaching;
            f.SeniorityOrder = model.SeniorityOrder;

            if (Photo != null && Photo.Length > 0)
            {
                var uploadResult = await _fileStorage.SaveAsync(Photo, "faculty", FileCategory.Image);
                if (!uploadResult.Success)
                {
                    ModelState.AddModelError(nameof(Photo), uploadResult.ErrorMessage!);
                    return View(await BuildEditVM(model));
                }
                // Save new before deleting old, so a failed upload never
                // leaves the faculty member with no photo at all.
                _fileStorage.Delete(f.ImagePath);
                f.ImagePath = uploadResult.RelativePath;
            }

            await _context.SaveChangesAsync();
            TempData["Success"] = $"Faculty '{f.Name}' updated.";

            _logger.LogInformation("Faculty {FacultyId} '{Name}' edited by {User}",
                f.FacultyId, f.Name, User.Identity?.Name);

            await TryNotifyAsync(
                title: $"Faculty Updated: {f.Name}",
                message: "Profile details were changed",
                icon: "fa-edit",
                color: "info",
                link: $"/Faculty/Edit/{f.FacultyId}");

            return RedirectToAction(nameof(Index));
        }

        // ── TOGGLE / DELETE / REORDER ─────────────────────
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "SuperAdmin,HOD")]
        public async Task<IActionResult> ToggleActive(int id)
        {
            var f = await _context.Faculties.FindAsync(id);
            if (f == null) return NotFound();

            if (!await CanManageFacultyAsync(id)) return Forbid();

            f.IsActive = !f.IsActive;
            await _context.SaveChangesAsync();
            TempData["Success"] = $"'{f.Name}' " + (f.IsActive ? "activated" : "deactivated") + ".";

            _logger.LogInformation("Faculty {FacultyId} '{Name}' {Action} by {User}",
                f.FacultyId, f.Name, f.IsActive ? "activated" : "deactivated", User.Identity?.Name);

            await TryNotifyAsync(
                title: $"Faculty {(f.IsActive ? "Activated" : "Deactivated")}: {f.Name}",
                message: null,
                icon: f.IsActive ? "fa-user-check" : "fa-user-slash",
                color: f.IsActive ? "success" : "warning",
                link: $"/Faculty/Edit/{f.FacultyId}");

            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "SuperAdmin")]
        public async Task<IActionResult> Delete(int id)
        {
            var f = await _context.Faculties.FindAsync(id);
            if (f == null) return NotFound();
            f.IsDeleted = true; f.IsActive = false;
            await _context.SaveChangesAsync();
            TempData["Success"] = $"Faculty '{f.Name}' deleted.";

            _logger.LogInformation("Faculty {FacultyId} '{Name}' deleted by {User}",
                f.FacultyId, f.Name, User.Identity?.Name);

            await TryNotifyAsync(
                title: $"Faculty Deleted: {f.Name}",
                message: null,
                icon: "fa-user-times",
                color: "danger",
                link: "/Faculty/Index");

            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "SuperAdmin,HOD")]
        public async Task<IActionResult> Reorder(int id, string direction)
        {
            var f = await _context.Faculties.FindAsync(id);
            if (f == null) return NotFound();

            if (!await CanManageFacultyAsync(id)) return Forbid();

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
            if (!await CanManageFacultyAsync(model.FacultyId)) return Forbid();

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
                await TryNotifyAsync(
                   title: $"Qualification Added for {faculty?.Name}",
                   message: $"{model.Degree} - {model.Specialization}",
                   icon: "fa-graduation-cap",
                   color: "info",
                   link: $"/Faculty/Edit/{model.FacultyId}");
            }
            else
            {
                TempData["Error"] = "Could not add qualification — please check the form and try again.";
            }
            return RedirectToAction(nameof(Qualifications), new { id = model.FacultyId });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteQualification(int id, int facultyId)
        {
            if (!await CanManageFacultyAsync(facultyId)) return Forbid();

            var item = await _context.FacultyQualifications.FindAsync(id);
            if (item != null)
            {
                // NOTE: previously item.Degree/item.Specialization were read
                // AFTER this if-block (outside the null check), which threw a
                // NullReferenceException whenever id didn't match an existing
                // record. Moved inside, matching the safe pattern already used
                // by DeleteExperience/DeleteTraining/DeletePublication below.
                var degree = item.Degree;
                var specialization = item.Specialization;

                item.IsDeleted = true;
                await _context.SaveChangesAsync();

                var facultyName = await GetFacultyNameAsync(facultyId);

                await TryNotifyAsync(
                    title: $"Qualification Removed for {facultyName}",
                    message: $"{degree} - {specialization}",
                    icon: "fa-user-minus",
                    color: "warning",
                    link: $"/Faculty/Edit/{facultyId}");
            }
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
            if (!await CanManageFacultyAsync(model.FacultyId)) return Forbid();

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
                var facultyName = await GetFacultyNameAsync(model.FacultyId);
                await _context.SaveChangesAsync();
                await TryNotifyAsync(
                    title: $"Experience Added for {facultyName}",
                    message: $"{model.Position} at {model.Organization}",
                    icon: "fa-briefcase",
                    color: "info",
                    link: $"/Faculty/Edit/{model.FacultyId}");

                TempData["Success"] = "Experience added.";
            }
            else
            {
                TempData["Error"] = "Could not add experience — please check the form and try again.";
            }
            return RedirectToAction(nameof(Experience), new { id = model.FacultyId });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteExperience(int id, int facultyId)
        {
            if (!await CanManageFacultyAsync(facultyId)) return Forbid();

            var item = await _context.FacultyExperiences.FindAsync(id);
            if (item != null)
            {
                var facultyName = await GetFacultyNameAsync(facultyId);

                var position = item.Position;
                var org = item.Organization;
                await TryNotifyAsync(
                    title: $"Experience Removed for {facultyName}",
                    message: $"{position} at {org}",
                    icon: "fa-user-minus",
                    color: "warning",
                    link: $"/Faculty/Edit/{facultyId}");
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
            if (!await CanManageFacultyAsync(model.FacultyId)) return Forbid();

            if (ModelState.IsValid)
            {
                _context.FacultyTrainings.Add(new FacultyTraining
                {
                    FacultyId = model.FacultyId,
                    Title = model.Title,
                    OrganizedBy = model.OrganizedBy,
                    TrainingType = model.TrainingType,
                    FromDate = model.FromDate,
                    ToDate = model.ToDate
                });
                await _context.SaveChangesAsync();

                var facultyName = await GetFacultyNameAsync(model.FacultyId);

                await TryNotifyAsync(
                    title: $"Training Added for {facultyName}",
                    message: $"{model.Title} by {model.OrganizedBy}",
                    icon: "fa-chalkboard-teacher",
                    color: "info",
                    link: $"/Faculty/Edit/{model.FacultyId}");
                TempData["Success"] = "Training added.";
            }
            else
            {
                TempData["Error"] = "Could not add training — please check the form and try again.";
            }
            return RedirectToAction(nameof(Training), new { id = model.FacultyId });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteTraining(int id, int facultyId)
        {
            if (!await CanManageFacultyAsync(facultyId)) return Forbid();

            var item = await _context.FacultyTrainings.FindAsync(id);
            if (item != null)
            {
                item.IsDeleted = true;
                await _context.SaveChangesAsync();
                var title = item.Title;
                var org = item.OrganizedBy;
                var facultyName = await GetFacultyNameAsync(facultyId);

                await TryNotifyAsync(
                    title: $"Training Removed for {facultyName}",
                    message: $"{title} by {org}",
                    icon: "fa-user-minus",
                    color: "warning",
                    link: $"/Faculty/Edit/{facultyId}");
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
            if (!await CanManageFacultyAsync(model.FacultyId)) return Forbid();

            if (ModelState.IsValid)
            {
                _context.FacultyPublications.Add(new FacultyPublication
                {
                    FacultyId = model.FacultyId,
                    SrNo = model.SrNo,
                    Title = model.Title,
                    Type = model.Type,

                });
                var facultyName = await GetFacultyNameAsync(model.FacultyId);
                await _context.SaveChangesAsync();
                await TryNotifyAsync(
                   title: $"Publication Added for {facultyName}",
                   message: $"[{model.SrNo}] {model.Title}",
                   icon: "fa-book",
                   color: "info",
                   link: $"/Faculty/Edit/{model.FacultyId}");
                TempData["Success"] = "Publication added.";
            }
            else
            {
                TempData["Error"] = "Could not add publication — please check the form and try again.";
            }
            return RedirectToAction(nameof(Publications), new { id = model.FacultyId });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeletePublication(int id, int facultyId)
        {
            if (!await CanManageFacultyAsync(facultyId)) return Forbid();

            var item = await _context.FacultyPublications.FindAsync(id);
            if (item != null)
            {
                var title = item.Title;
                var srNo = item.SrNo;
                item.IsDeleted = true; await _context.SaveChangesAsync();
                var facultyName = await GetFacultyNameAsync(facultyId);

                await TryNotifyAsync(
                    title: $"Publication Removed for {facultyName}",
                    message: $"[{srNo}] {title}",
                    icon: "fa-trash",
                    color: "warning",
                    link: $"/Faculty/Edit/{facultyId}");
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
                DateOfBirth = detail?.DateOfBirth,
                Department = detail?.Department,
                Contact = detail?.Contact,
                Email = detail?.Email
            });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SavePersonalDetails(PersonalDetailVM model)
        {
            if (!await CanManageFacultyAsync(model.FacultyId)) return Forbid();

            var existing = await _context.PersonalDetails
                .FirstOrDefaultAsync(p => p.FacultyId == model.FacultyId);
            if (existing == null)
                _context.PersonalDetails.Add(new PersonalDetail
                {
                    FacultyId = model.FacultyId,
                    Department = model.Department,
                    Contact = model.Contact,
                    Email = model.Email,
                    DateOfBirth = model.DateOfBirth
                });
            else
            {
                existing.Department = model.Department;
                existing.Contact = model.Contact;
                existing.Email = model.Email;
                existing.DateOfBirth = model.DateOfBirth;
            }
            await _context.SaveChangesAsync();
            var facultyName = await GetFacultyNameAsync(model.FacultyId);

            await TryNotifyAsync(
                title: $"Personal Details Updated",
                message: $"{facultyName}'s profile updated",
                icon: "fa-id-card",
                color: "primary",
                link: $"/Faculty/Edit/{model.FacultyId}");
            TempData["Success"] = "Personal details saved.";
            return RedirectToAction(nameof(PersonalDetails), new { id = model.FacultyId });
        }
        public async Task<IActionResult> Profile(int id)
        {
            ViewData["Title"] = "Faculty Profile";

            var faculty = await _context.Faculties
                .Include(f => f.Department)
                .Include(f => f.Qualifications)
                .Include(f => f.Experiences)
                .Include(f => f.Trainings)
                .Include(f => f.Publications)
                .Include(f => f.Subjects)
                .Include(f => f.ResearchGuidances)
                .Include(f => f.BookPublications)
                .Include(f => f.Consultancies)
                .Include(f => f.Patents)
                .Include(f => f.ProfessionalMemberships)
                .Include(f => f.PersonalDetail)
                .FirstOrDefaultAsync(f => f.FacultyId == id);

            if (faculty == null) return NotFound();

            return View(faculty);
        }

        // ── SUBJECTS (UG/PG) ──
        public async Task<IActionResult> Subjects(int id)
        {
            ViewData["Title"] = "Subjects Taught";
            var subjects = await _context.FacultySubjects
                .Where(s => s.FacultyId == id)
                .OrderBy(s => s.Level).ThenBy(s => s.DisplayOrder)
                .ToListAsync();
            ViewBag.FacultyId = id;
            ViewBag.FacultyName = (await _context.Faculties.FindAsync(id))?.Name;
            return View(subjects);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AddSubject(int FacultyId, string SubjectName, string Level)
        {
            if (!await CanManageFacultyAsync(FacultyId)) return Forbid();

            if (!string.IsNullOrWhiteSpace(SubjectName))
            {
                _context.FacultySubjects.Add(new FacultySubject
                {
                    FacultyId = FacultyId,
                    SubjectName = SubjectName.Trim(),
                    Level = Level ?? "UG"
                });
                await _context.SaveChangesAsync();
                TempData["Success"] = "Subject added.";
            }
            return RedirectToAction("Subjects", new { id = FacultyId });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteSubject(int id, int facultyId)
        {
            if (!await CanManageFacultyAsync(facultyId)) return Forbid();

            var s = await _context.FacultySubjects.FindAsync(id);
            if (s != null) { s.IsDeleted = true; await _context.SaveChangesAsync(); }
            return RedirectToAction("Subjects", new { id = facultyId });
        }

        // ── RESEARCH GUIDANCE ──
        public async Task<IActionResult> ResearchGuidance(int id)
        {
            ViewData["Title"] = "Research Guidance";
            var guidance = await _context.FacultyResearchGuidances
                .Where(r => r.FacultyId == id).ToListAsync();
            ViewBag.FacultyId = id;
            ViewBag.FacultyName = (await _context.Faculties.FindAsync(id))?.Name;
            return View(guidance);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SaveResearchGuidance(
            int FacultyId, string Level,
            int Ongoing, int Completed)
        {
            if (!await CanManageFacultyAsync(FacultyId)) return Forbid();

            var existing = await _context.FacultyResearchGuidances
                .FirstOrDefaultAsync(r => r.FacultyId == FacultyId
                                       && r.Level == Level);
            if (existing == null)
            {
                _context.FacultyResearchGuidances.Add(new FacultyResearchGuidance
                {
                    FacultyId = FacultyId,
                    Level = Level,
                    Ongoing = Ongoing,
                    Completed = Completed
                });
            }
            else
            {
                existing.Ongoing = Ongoing;
                existing.Completed = Completed;
            }
            await _context.SaveChangesAsync();
            TempData["Success"] = "Research guidance updated.";
            return RedirectToAction("ResearchGuidance", new { id = FacultyId });
        }

        // ── BOOK PUBLICATIONS ──
        public async Task<IActionResult> BookPublications(int id)
        {
            ViewData["Title"] = "Book Publications";
            var books = await _context.FacultyBookPublications
                .Where(b => b.FacultyId == id)
                .OrderBy(b => b.SrNo).ToListAsync();
            ViewBag.FacultyId = id;
            ViewBag.FacultyName = (await _context.Faculties.FindAsync(id))?.Name;
            return View(books);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AddBookPublication(
            int FacultyId, string Title, string? Publisher, string? Year)
        {
            if (!await CanManageFacultyAsync(FacultyId)) return Forbid();

            if (!string.IsNullOrWhiteSpace(Title))
            {
                int srNo = await _context.FacultyBookPublications
                    .Where(b => b.FacultyId == FacultyId)
                    .CountAsync() + 1;

                _context.FacultyBookPublications.Add(new FacultyBookPublication
                {
                    FacultyId = FacultyId,
                    SrNo = srNo,
                    Title = Title.Trim(),
                    Publisher = Publisher,
                    Year = Year
                });
                await _context.SaveChangesAsync();
                TempData["Success"] = "Book publication added.";
            }
            return RedirectToAction("BookPublications", new { id = FacultyId });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteBookPublication(int id, int facultyId)
        {
            if (!await CanManageFacultyAsync(facultyId)) return Forbid();

            var b = await _context.FacultyBookPublications.FindAsync(id);
            if (b != null) { b.IsDeleted = true; await _context.SaveChangesAsync(); }
            return RedirectToAction("BookPublications", new { id = facultyId });
        }

        // ── PATENTS ──
        public async Task<IActionResult> Patents(int id)
        {
            ViewData["Title"] = "Patents";
            var patents = await _context.FacultyPatents
                .Where(p => p.FacultyId == id)
                .OrderBy(p => p.DisplayOrder).ToListAsync();
            ViewBag.FacultyId = id;
            ViewBag.FacultyName = (await _context.Faculties.FindAsync(id))?.Name;
            return View(patents);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AddPatent(
            int FacultyId, string Title,
            string? ApplicationNo, string? GrantedYear, string Status)
        {
            if (!await CanManageFacultyAsync(FacultyId)) return Forbid();

            if (!string.IsNullOrWhiteSpace(Title))
            {
                _context.FacultyPatents.Add(new FacultyPatent
                {
                    FacultyId = FacultyId,
                    Title = Title.Trim(),
                    ApplicationNo = ApplicationNo,
                    GrantedYear = GrantedYear,
                    Status = Status ?? "Filed"
                });
                await _context.SaveChangesAsync();
                TempData["Success"] = "Patent added.";
            }
            return RedirectToAction("Patents", new { id = FacultyId });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeletePatent(int id, int facultyId)
        {
            if (!await CanManageFacultyAsync(facultyId)) return Forbid();

            var p = await _context.FacultyPatents.FindAsync(id);
            if (p != null) { p.IsDeleted = true; await _context.SaveChangesAsync(); }
            return RedirectToAction("Patents", new { id = facultyId });
        }

        // ── PROFESSIONAL MEMBERSHIPS ──
        public async Task<IActionResult> Memberships(int id)
        {
            ViewData["Title"] = "Professional Memberships";
            var memberships = await _context.FacultyProfessionalMemberships
                .Where(m => m.FacultyId == id)
                .OrderBy(m => m.DisplayOrder).ToListAsync();
            ViewBag.FacultyId = id;
            ViewBag.FacultyName = (await _context.Faculties.FindAsync(id))?.Name;
            return View(memberships);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AddMembership(
            int FacultyId, string Community, string MembershipType)
        {
            if (!await CanManageFacultyAsync(FacultyId)) return Forbid();

            if (!string.IsNullOrWhiteSpace(Community))
            {
                _context.FacultyProfessionalMemberships.Add(
                    new FacultyProfessionalMembership
                    {
                        FacultyId = FacultyId,
                        Community = Community.Trim(),
                        MembershipType = MembershipType ?? "Member"
                    });
                await _context.SaveChangesAsync();
                TempData["Success"] = "Membership added.";
            }
            return RedirectToAction("Memberships", new { id = FacultyId });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteMembership(int id, int facultyId)
        {
            if (!await CanManageFacultyAsync(facultyId)) return Forbid();

            var m = await _context.FacultyProfessionalMemberships.FindAsync(id);
            if (m != null) { m.IsDeleted = true; await _context.SaveChangesAsync(); }
            return RedirectToAction("Memberships", new { id = facultyId });
        }

        // ================= CONSULTANCY =================

        // INDEX
        public async Task<IActionResult> Consultancy(int id)
        {
            int facultyId = id;

            ViewData["Title"] = "Consultancy Projects";

            var faculty = await _context.Faculties
                .Include(f => f.Department)
                .FirstOrDefaultAsync(f => f.FacultyId == facultyId);

            if (faculty == null)
                return NotFound();

            ViewBag.Faculty = faculty;
            ViewBag.FacultyId = facultyId;
            var items = await _context.FacultyConsultancies
                .Where(c => c.FacultyId == facultyId)
                .OrderBy(c => c.DisplayOrder)
                .ThenByDescending(c => c.Year)
                .Select(c => new FacultyConsultancyVM
                {
                    Id = c.FacultyConsultancyId,
                    FacultyId = c.FacultyId,
                    Title = c.Title,
                    Client = c.Client,
                    Year = c.Year,
                    Amount = c.Amount,
                    DisplayOrder = c.DisplayOrder
                })
                .ToListAsync();

            return View(items);
        }

        // CREATE GET
        public async Task<IActionResult> CreateConsultancy(int facultyId)
        {
            var faculty = await _context.Faculties
                .FirstOrDefaultAsync(f => f.FacultyId == facultyId);

            if (faculty == null)
                return NotFound();

            ViewBag.Faculty = faculty;
            ViewBag.FacultyId = facultyId;

            return View(new FacultyConsultancyVM
            {
                FacultyId = facultyId
            });
        }

        // CREATE POST
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateConsultancy(FacultyConsultancyVM vm)
        {
            if (!await CanManageFacultyAsync(vm.FacultyId)) return Forbid();

            if (!ModelState.IsValid)
            {
                ViewBag.Faculty = await _context.Faculties
                    .Include(f => f.Department)
                    .FirstOrDefaultAsync(f => f.FacultyId == vm.FacultyId);

                ViewBag.FacultyId = vm.FacultyId;

                return View(vm);
            }
            var entity = new FacultyConsultancy
            {
                FacultyId = vm.FacultyId,
                Title = vm.Title,
                Client = vm.Client,
                Year = vm.Year,
                Amount = vm.Amount,
                DisplayOrder = vm.DisplayOrder
            };

            _context.FacultyConsultancies.Add(entity);
            await _context.SaveChangesAsync();

            return RedirectToAction(nameof(Consultancy), new { id = vm.FacultyId });
        }

        // EDIT GET
        public async Task<IActionResult> EditConsultancy(int id)
        {
            var item = await _context.FacultyConsultancies.FindAsync(id);

            if (item == null)
                return NotFound();

            var faculty = await _context.Faculties
                .Include(f => f.Department)
                .FirstOrDefaultAsync(f => f.FacultyId == item.FacultyId);

            if (faculty == null)
                return NotFound();

            ViewBag.Faculty = faculty;
            ViewBag.FacultyId = faculty.FacultyId;

            var vm = new FacultyConsultancyVM
            {
                Id = item.FacultyConsultancyId,
                FacultyId = item.FacultyId,
                Title = item.Title,
                Client = item.Client,
                Year = item.Year,
                Amount = item.Amount,
                DisplayOrder = item.DisplayOrder
            };

            return View("CreateConsultancy", vm);
        }
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditConsultancy(FacultyConsultancyVM vm)
        {
            if (!await CanManageFacultyAsync(vm.FacultyId)) return Forbid();

            if (!ModelState.IsValid)
                return View("CreateConsultancy", vm);

            var entity = await _context.FacultyConsultancies.FindAsync(vm.Id);

            if (entity == null)
                return NotFound();

            entity.Title = vm.Title;
            entity.Client = vm.Client;
            entity.Year = vm.Year;
            entity.Amount = vm.Amount;
            entity.DisplayOrder = vm.DisplayOrder;

            await _context.SaveChangesAsync();

            return RedirectToAction(nameof(Consultancy), new { id = vm.FacultyId });
        }
        // DELETE
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConsultancy(int id)
        {
            var item = await _context.FacultyConsultancies.FindAsync(id);

            if (item == null)
                return NotFound();

            if (!await CanManageFacultyAsync(item.FacultyId)) return Forbid();

            int facultyId = item.FacultyId;

            // Soft delete, matching every other sub-resource in this
            // controller (Qualifications/Experience/Training/Publications/
            // Subjects/ResearchGuidance/BookPublications/Patents/
            // Memberships) and the codebase-wide pattern — this entity
            // already has the same HasQueryFilter(!IsDeleted) as the others,
            // so a hard Remove() here was an inconsistency, not a requirement.
            item.IsDeleted = true;
            await _context.SaveChangesAsync();

            return RedirectToAction(nameof(Consultancy), new { id = facultyId });
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

        private async Task<ApplicationUser?> GetCurrentUserAsync()
        {
            var userId = User.FindFirst(
                System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
            if (userId == null) return null;
            return await _context.Users.FindAsync(userId);
        }

        /// <summary>
        /// The "look up this faculty member's name for a notification message"
        /// query was copy-pasted ~8 times across the sub-resource actions
        /// below. One place to change it if the notification format ever needs
        /// more than just the name (e.g. department).
        /// </summary>
        private async Task<string?> GetFacultyNameAsync(int facultyId) =>
            await _context.Faculties
                .Where(f => f.FacultyId == facultyId)
                .Select(f => f.Name)
                .FirstOrDefaultAsync();

        /// <summary>
        /// True if the current user is allowed to add/edit/delete records on
        /// the given faculty member's profile — SuperAdmin/Principal always,
        /// the HOD of that faculty's own department, or the Faculty user
        /// whose own profile it is.
        ///
        /// Every sub-resource mutating action in this controller (add/delete
        /// qualifications, experience, training, publications, personal
        /// details, subjects, research guidance, book publications, patents,
        /// memberships, consultancy) MUST call this before making any
        /// change. Previously NONE of them did — the only protection was the
        /// class-level [Authorize(Roles = "...,Faculty")], which means any
        /// authenticated Faculty user could add or delete records on ANY
        /// OTHER faculty member's profile just by changing the FacultyId in
        /// the submitted form. This is the single most important fix in this
        /// file — logged on denial so attempted misuse is visible.
        /// </summary>
        private async Task<bool> CanManageFacultyAsync(int facultyId)
        {
            if (User.IsInRole(AppRoles.SuperAdmin) || User.IsInRole(AppRoles.Principal))
                return true;

            var cu = await GetCurrentUserAsync();
            if (cu == null) return false;

            if (User.IsInRole(AppRoles.HOD))
            {
                var facultyDeptId = await _context.Faculties
                    .Where(f => f.FacultyId == facultyId)
                    .Select(f => (int?)f.DeptId)
                    .FirstOrDefaultAsync();

                if (facultyDeptId != null && facultyDeptId == cu.DeptId)
                    return true;
            }

            if (User.IsInRole(AppRoles.Faculty) && cu.FacultyId == facultyId)
                return true;

            _logger.LogWarning(
                "User {User} (roles via claims) was denied access to manage faculty {FacultyId}",
                User.Identity?.Name, facultyId);
            return false;
        }

        /// <summary>
        /// Sends an admin notification for a Faculty-module event, but never
        /// lets a notification failure fail the request it's attached to —
        /// the underlying data change has already been saved by the time
        /// this is called, so a NotificationService problem should be logged,
        /// not surfaced as an error on an otherwise-successful save.
        /// </summary>
        private async Task TryNotifyAsync(string title, string? message, string icon, string color, string link)
        {
            try
            {
                await _notify.SendAsync(
                    title: title,
                    message: message,
                    module: "Faculty",
                    icon: icon,
                    color: color,
                    link: link,
                    forRole: "SuperAdmin");
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to send Faculty module notification: {Title}", title);
            }
        }
    }
}