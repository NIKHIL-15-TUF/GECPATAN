using GECPatan.Core.Data;
using  GECPatan.Core.Models.Domain;
using GECPatan.Admin.Models.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace GECPatan.Admin.Controllers
{
    [Authorize(Roles = "SuperAdmin,Principal")]
    public class PrincipalController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly IWebHostEnvironment _env;

        public PrincipalController(ApplicationDbContext context,
            IWebHostEnvironment env)
        {
            _context = context;
            _env = env;
        }

        // ── INDEX — list all (active + past) ──────────────
        public async Task<IActionResult> Index()
        {
            ViewData["Title"] = "Principal";

            var list = await _context.Principals
                .IgnoreQueryFilters()   // show soft-deleted too
                .Where(p => !p.IsDeleted)
                .OrderByDescending(p => p.IsActive)
                .ThenByDescending(p => p.DateOfJoiningInstitute)
                .Select(p => new PrincipalListVM
                {
                    Id = p.Id,
                    Name = p.Name,
                    Designation = p.Designation,
                    PhotoPath = p.PhotoPath,
                    IsActive = p.IsActive,
                    DateOfJoiningInstitute = p.DateOfJoiningInstitute,
                    TransferDate = p.TransferDate,
                    TransferNote = p.TransferNote
                })
                .ToListAsync();

            return View(list);
        }

        // ── CREATE GET ────────────────────────────────────
        [Authorize(Roles = "SuperAdmin")]
        public IActionResult Create()
        {
            ViewData["Title"] = "Add Principal";
            return View("Form", new PrincipalFormVM());
        }

        // ── CREATE POST ───────────────────────────────────
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "SuperAdmin")]
        public async Task<IActionResult> Create(
            PrincipalFormVM model, IFormFile? Photo)
        {
            ViewData["Title"] = "Add Principal";
            if (!ModelState.IsValid) return View("Form", model);

            // Deactivate current active principal first
            var current = await _context.Principals
                .FirstOrDefaultAsync(p => p.IsActive);
            if (current != null)
            {
                TempData["Error"] =
                    $"'{current.Name}' is currently the active principal. " +
                    "Please transfer them first before adding a new one.";
                return RedirectToAction(nameof(Index));
            }

            var principal = new Principal
            {
                Name = model.Name,
                Designation = model.Designation,
                Email = model.Email,
                Contact = model.Contact,
                Message = model.Message,
                AreaOfInterest = model.AreaOfInterest,
                DateOfJoiningInstitute = model.DateOfJoiningInstitute,
                DateOfJoiningDept = model.DateOfJoiningDept,
                IsActive = true
            };

            if (Photo != null && Photo.Length > 0)
                principal.PhotoPath = await SaveFileAsync(Photo, "principal");

            _context.Principals.Add(principal);
            await _context.SaveChangesAsync();

            TempData["Success"] =
                $"Principal '{principal.Name}' added successfully.";
            return RedirectToAction(nameof(Edit), new { id = principal.Id });
        }

        // ── EDIT GET ──────────────────────────────────────
        public async Task<IActionResult> Edit(int id)
        {
            ViewData["Title"] = "Edit Principal Profile";

            var p = await _context.Principals
                .IgnoreQueryFilters()
                .FirstOrDefaultAsync(x => x.Id == id);

            if (p == null) return NotFound();

            return View("Form", new PrincipalFormVM
            {
                Id = p.Id,
                Name = p.Name,
                Designation = p.Designation,
                Email = p.Email,
                Contact = p.Contact,
                ExistingPhotoPath = p.PhotoPath,
                Message = p.Message,
                AreaOfInterest = p.AreaOfInterest,
                DateOfJoiningInstitute = p.DateOfJoiningInstitute,
                DateOfJoiningDept = p.DateOfJoiningDept,
                IsActive = p.IsActive
            });
        }

        // ── EDIT POST ─────────────────────────────────────
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(
            int id, PrincipalFormVM model, IFormFile? Photo)
        {
            ViewData["Title"] = "Edit Principal Profile";
            if (!ModelState.IsValid) return View("Form", model);

            var p = await _context.Principals
                .IgnoreQueryFilters()
                .FirstOrDefaultAsync(x => x.Id == id);

            if (p == null) return NotFound();

            p.Name = model.Name;
            p.Designation = model.Designation;
            p.Email = model.Email;
            p.Contact = model.Contact;
            p.Message = model.Message;
            p.AreaOfInterest = model.AreaOfInterest;
            p.DateOfJoiningInstitute = model.DateOfJoiningInstitute;
            p.DateOfJoiningDept = model.DateOfJoiningDept;

            if (Photo != null && Photo.Length > 0)
            {
                DeleteFile(p.PhotoPath);
                p.PhotoPath = await SaveFileAsync(Photo, "principal");
            }

            await _context.SaveChangesAsync();
            TempData["Success"] = "Principal profile updated.";
            return RedirectToAction(nameof(Edit), new { id });
        }

        // ── PREVIEW ───────────────────────────────────────
        public async Task<IActionResult> Preview(int id)
        {
            var p = await _context.Principals
                .IgnoreQueryFilters()
                .Include(x => x.Qualifications.OrderBy(q => q.DisplayOrder))
                .Include(x => x.Experiences.OrderBy(e => e.DisplayOrder))
                .Include(x => x.Publications.OrderBy(pub => pub.DisplayOrder))
                .Include(x => x.BookPublications.OrderBy(b => b.DisplayOrder))
                .Include(x => x.ExpertTalks.OrderBy(e => e.DisplayOrder))
                .Include(x => x.Achievements.OrderBy(a => a.DisplayOrder))
                .Include(x => x.Memberships.OrderBy(m => m.DisplayOrder))
                .FirstOrDefaultAsync(x => x.Id == id);

            if (p == null) return NotFound();

            // Get dynamic sections for this principal
            ViewBag.DynamicSections = await _context.DynamicSections
                .Include(s => s.Files)
                .Where(s => s.PageType == PageType.Principal
                         && s.PageId == id
                         && !s.IsDeleted)
                .OrderBy(s => s.DisplayOrder)
                .ToListAsync();

            ViewData["Title"] = $"Preview — {p.Name}";
            return View(p);
        }

        // ══════════════════════════════════════════════════
        // TRANSFER FLOW
        // ══════════════════════════════════════════════════

        [Authorize(Roles = "SuperAdmin")]
        public async Task<IActionResult> Transfer()
        {
            ViewData["Title"] = "Transfer / Replace Principal";

            var current = await _context.Principals
                .FirstOrDefaultAsync(p => p.IsActive);

            if (current == null)
            {
                TempData["Error"] = "No active principal found.";
                return RedirectToAction(nameof(Index));
            }

            return View(new PrincipalTransferVM
            {
                CurrentPrincipalId = current.Id,
                CurrentPrincipalName = current.Name,
                TransferDate = DateTime.Today
            });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "SuperAdmin")]
        public async Task<IActionResult> Transfer(PrincipalTransferVM model)
        {
            ViewData["Title"] = "Transfer / Replace Principal";
            if (!ModelState.IsValid) return View(model);

            var current = await _context.Principals
                .FindAsync(model.CurrentPrincipalId);

            if (current == null) return NotFound();

            // Deactivate current principal
            current.IsActive = false;
            current.TransferDate = model.TransferDate;
            current.TransferNote = model.TransferNote;

            await _context.SaveChangesAsync();

            TempData["Success"] =
                $"'{current.Name}' has been marked as transferred. " +
                "You can now add the new principal.";

            return RedirectToAction(nameof(Create));
        }

        // ══════════════════════════════════════════════════
        // SUB SECTIONS
        // ══════════════════════════════════════════════════

        // ── QUALIFICATIONS ────────────────────────────────
        public async Task<IActionResult> Qualifications(int id)
        {
            ViewData["Title"] = "Qualifications";
            await SetPrincipalViewBag(id);
            var list = await _context.PrincipalQualifications
                .Where(q => q.PrincipalId == id)
                .OrderBy(q => q.DisplayOrder)
                .ToListAsync();
            return View(list);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AddQualification(
            PrincipalQualificationVM model)
        {
            if (ModelState.IsValid)
            {
                _context.PrincipalQualifications.Add(new PrincipalQualification
                {
                    PrincipalId = model.PrincipalId,
                    Degree = model.Degree,
                    University = model.University,
                    Year = model.Year,
                    Result = model.Result,
                    DisplayOrder = model.DisplayOrder
                });
                await _context.SaveChangesAsync();
                TempData["Success"] = "Qualification added.";
            }
            return RedirectToAction(nameof(Qualifications),
                new { id = model.PrincipalId });
        }

        [HttpPost]
        public async Task<IActionResult> DeleteQualification(
            int id, int principalId)
        {
            var item = await _context.PrincipalQualifications.FindAsync(id);
            if (item != null)
            {
                item.IsDeleted = true;
                await _context.SaveChangesAsync();
            }
            return RedirectToAction(nameof(Qualifications),
                new { id = principalId });
        }

        // ── EXPERIENCE ────────────────────────────────────
        public async Task<IActionResult> Experience(int id)
        {
            ViewData["Title"] = "Experience";
            await SetPrincipalViewBag(id);
            var list = await _context.PrincipalExperiences
                .Where(e => e.PrincipalId == id)
                .OrderBy(e => e.DisplayOrder)
                .ToListAsync();
            return View(list);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AddExperience(
            PrincipalExperienceVM model)
        {
            if (ModelState.IsValid)
            {
                _context.PrincipalExperiences.Add(new PrincipalExperience
                {
                    PrincipalId = model.PrincipalId,
                    Designation = model.Designation,
                    Organization = model.Organization,
                    Place = model.Place,
                    FromDate = model.FromDate,
                    ToDate = model.ToDate,
                    DisplayOrder = model.DisplayOrder
                });
                await _context.SaveChangesAsync();
                TempData["Success"] = "Experience added.";
            }
            return RedirectToAction(nameof(Experience),
                new { id = model.PrincipalId });
        }

        [HttpPost]
        public async Task<IActionResult> DeleteExperience(
            int id, int principalId)
        {
            var item = await _context.PrincipalExperiences.FindAsync(id);
            if (item != null) { item.IsDeleted = true; await _context.SaveChangesAsync(); }
            return RedirectToAction(nameof(Experience),
                new { id = principalId });
        }

        // ── PUBLICATIONS ──────────────────────────────────
        public async Task<IActionResult> Publications(int id)
        {
            ViewData["Title"] = "Publications";
            await SetPrincipalViewBag(id);
            var list = await _context.PrincipalPublications
                .Where(p => p.PrincipalId == id)
                .OrderBy(p => p.DisplayOrder)
                .ToListAsync();
            return View(list);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AddPublication(
            PrincipalPublicationVM model)
        {
            if (ModelState.IsValid)
            {
                _context.PrincipalPublications.Add(new PrincipalPublication
                {
                    PrincipalId = model.PrincipalId,
                    Title = model.Title,
                    JournalOrConference = model.JournalOrConference,
                    Type = model.Type,
                    DOI = model.DOI,
                    Year = model.Year,
                    DisplayOrder = model.DisplayOrder
                });
                await _context.SaveChangesAsync();
                TempData["Success"] = "Publication added.";
            }
            return RedirectToAction(nameof(Publications),
                new { id = model.PrincipalId });
        }

        [HttpPost]
        public async Task<IActionResult> DeletePublication(
            int id, int principalId)
        {
            var item = await _context.PrincipalPublications.FindAsync(id);
            if (item != null) { item.IsDeleted = true; await _context.SaveChangesAsync(); }
            return RedirectToAction(nameof(Publications),
                new { id = principalId });
        }

        // ── BOOK PUBLICATIONS ─────────────────────────────
        public async Task<IActionResult> BookPublications(int id)
        {
            ViewData["Title"] = "Book Publications";
            await SetPrincipalViewBag(id);
            var list = await _context.PrincipalBookPublications
                .Where(b => b.PrincipalId == id)
                .OrderBy(b => b.DisplayOrder)
                .ToListAsync();
            return View(list);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AddBookPublication(
            PrincipalBookPublicationVM model)
        {
            if (ModelState.IsValid)
            {
                _context.PrincipalBookPublications.Add(
                    new PrincipalBookPublication
                    {
                        PrincipalId = model.PrincipalId,
                        Title = model.Title,
                        BookCode = model.BookCode,
                        University = model.University,
                        Branch = model.Branch,
                        Semester = model.Semester,
                        ISBN = model.ISBN,
                        Publisher = model.Publisher,
                        ContentTopics = model.ContentTopics,
                        DisplayOrder = model.DisplayOrder
                    });
                await _context.SaveChangesAsync();
                TempData["Success"] = "Book publication added.";
            }
            return RedirectToAction(nameof(BookPublications),
                new { id = model.PrincipalId });
        }

        [HttpPost]
        public async Task<IActionResult> DeleteBookPublication(
            int id, int principalId)
        {
            var item = await _context.PrincipalBookPublications.FindAsync(id);
            if (item != null) { item.IsDeleted = true; await _context.SaveChangesAsync(); }
            return RedirectToAction(nameof(BookPublications),
                new { id = principalId });
        }

        // ── EXPERT TALKS ──────────────────────────────────
        public async Task<IActionResult> ExpertTalks(int id)
        {
            ViewData["Title"] = "Expert Talks";
            await SetPrincipalViewBag(id);
            var list = await _context.PrincipalExpertTalks
                .Where(e => e.PrincipalId == id)
                .OrderBy(e => e.DisplayOrder)
                .ToListAsync();
            return View(list);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AddExpertTalk(
            PrincipalExpertTalkVM model)
        {
            if (ModelState.IsValid)
            {
                _context.PrincipalExpertTalks.Add(new PrincipalExpertTalk
                {
                    PrincipalId = model.PrincipalId,
                    Year = model.Year,
                    Subject = model.Subject,
                    Place = model.Place,
                    Details = model.Details,
                    DisplayOrder = model.DisplayOrder
                });
                await _context.SaveChangesAsync();
                TempData["Success"] = "Expert talk added.";
            }
            return RedirectToAction(nameof(ExpertTalks),
                new { id = model.PrincipalId });
        }

        [HttpPost]
        public async Task<IActionResult> DeleteExpertTalk(
            int id, int principalId)
        {
            var item = await _context.PrincipalExpertTalks.FindAsync(id);
            if (item != null) { item.IsDeleted = true; await _context.SaveChangesAsync(); }
            return RedirectToAction(nameof(ExpertTalks),
                new { id = principalId });
        }

        // ── ACHIEVEMENTS ──────────────────────────────────
        public async Task<IActionResult> Achievements(int id)
        {
            ViewData["Title"] = "Achievements";
            await SetPrincipalViewBag(id);
            var list = await _context.PrincipalAchievements
                .Where(a => a.PrincipalId == id)
                .OrderBy(a => a.DisplayOrder)
                .ToListAsync();
            return View(list);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AddAchievement(
            PrincipalAchievementVM model)
        {
            if (ModelState.IsValid)
            {
                _context.PrincipalAchievements.Add(new PrincipalAchievement
                {
                    PrincipalId = model.PrincipalId,
                    AchievementText = model.AchievementText,
                    Year = model.Year,
                    DisplayOrder = model.DisplayOrder
                });
                await _context.SaveChangesAsync();
                TempData["Success"] = "Achievement added.";
            }
            return RedirectToAction(nameof(Achievements),
                new { id = model.PrincipalId });
        }

        [HttpPost]
        public async Task<IActionResult> DeleteAchievement(
            int id, int principalId)
        {
            var item = await _context.PrincipalAchievements.FindAsync(id);
            if (item != null) { item.IsDeleted = true; await _context.SaveChangesAsync(); }
            return RedirectToAction(nameof(Achievements),
                new { id = principalId });
        }

        // ── MEMBERSHIPS ───────────────────────────────────
        public async Task<IActionResult> Memberships(int id)
        {
            ViewData["Title"] = "Memberships";
            await SetPrincipalViewBag(id);
            var list = await _context.PrincipalMemberships
                .Where(m => m.PrincipalId == id)
                .OrderBy(m => m.DisplayOrder)
                .ToListAsync();
            return View(list);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AddMembership(
            PrincipalMembershipVM model)
        {
            if (ModelState.IsValid)
            {
                _context.PrincipalMemberships.Add(new PrincipalMembership
                {
                    PrincipalId = model.PrincipalId,
                    MembershipText = model.MembershipText,
                    DisplayOrder = model.DisplayOrder
                });
                await _context.SaveChangesAsync();
                TempData["Success"] = "Membership added.";
            }
            return RedirectToAction(nameof(Memberships),
                new { id = model.PrincipalId });
        }

        [HttpPost]
        public async Task<IActionResult> DeleteMembership(
            int id, int principalId)
        {
            var item = await _context.PrincipalMemberships.FindAsync(id);
            if (item != null) { item.IsDeleted = true; await _context.SaveChangesAsync(); }
            return RedirectToAction(nameof(Memberships),
                new { id = principalId });
        }

        // ══════════════════════════════════════════════════
        // HELPERS
        // ══════════════════════════════════════════════════
        private async Task SetPrincipalViewBag(int id)
        {
            var p = await _context.Principals
                .IgnoreQueryFilters()
                .FirstOrDefaultAsync(x => x.Id == id);
            ViewBag.PrincipalId = id;
            ViewBag.PrincipalName = p?.Name ?? "";
            ViewBag.IsActive = p?.IsActive ?? false;
        }

        private async Task<string> SaveFileAsync(
            IFormFile file, string folder)
        {
            var uploadsFolder = Path.Combine(
                _env.WebRootPath, "uploads", folder);
            Directory.CreateDirectory(uploadsFolder);
            var fileName = Guid.NewGuid()
                + Path.GetExtension(file.FileName);
            var filePath = Path.Combine(uploadsFolder, fileName);
            using var stream = new FileStream(filePath, FileMode.Create);
            await file.CopyToAsync(stream);
            return $"/uploads/{folder}/{fileName}";
        }

        private void DeleteFile(string? filePath)
        {
            if (string.IsNullOrEmpty(filePath)) return;
            var fullPath = Path.Combine(
                _env.WebRootPath, filePath.TrimStart('/'));
            if (System.IO.File.Exists(fullPath))
                System.IO.File.Delete(fullPath);
        }
    }
}