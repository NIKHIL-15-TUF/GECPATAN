using GECPatan.Admin.Models.ViewModels;
using GECPatan.Core.Data;
using GECPatan.Core.Models.Domain;
using GECPatan.Core.Services.FileStorage;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;

namespace GECPatan.Admin.Controllers
{
    [Authorize(Roles = "SuperAdmin,ContentEditor,HOD")]
    public class StudentClubController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly IFileStorageService _fileStorage;
        private readonly ILogger<StudentClubController> _logger;
        private const string ClubsFolder = "clubs";
        private const string ClubMembersFolder = "clubs/members";

        public StudentClubController(
            ApplicationDbContext context,
            IFileStorageService fileStorage,
            ILogger<StudentClubController> logger)
        {
            _context = context;
            _fileStorage = fileStorage;
            _logger = logger;
        }

        public async Task<IActionResult> Index()
        {
            ViewData["Title"] = "Student Clubs";
            var clubs = await _context.StudentClubs
                .Include(c => c.Images)
                .Include(c => c.Members)
                .OrderBy(c => c.DisplayOrder)
                .Select(c => new StudentClubListVM
                {
                    Id = c.Id,
                    Title = c.Title,
                    ImageCount = c.Images.Count,
                    IsVisible = c.IsVisible,
                    DisplayOrder = c.DisplayOrder,
                    MemberCount = c.Members.Count
                })
                .ToListAsync();
            return View(clubs);
        }

        public IActionResult Create()
        {
            ViewData["Title"] = "Add Club";
            return View(new StudentClubCreateVM());
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(StudentClubCreateVM model)
        {
            ViewData["Title"] = "Add Club";
            if (!ModelState.IsValid) return View(model);

            string? coverImagePath = null;
            if (model.CoverImageFile is { Length: > 0 })
            {
                var uploadResult = await _fileStorage.SaveAsync(model.CoverImageFile, ClubsFolder, FileCategory.Image);
                if (!uploadResult.Success)
                {
                    ModelState.AddModelError(nameof(model.CoverImageFile), uploadResult.ErrorMessage!);
                    return View(model);
                }
                coverImagePath = uploadResult.RelativePath;
            }

            int maxOrder = await _context.StudentClubs
                .Select(c => (int?)c.DisplayOrder).MaxAsync() ?? -1;

            var club = new StudentClub
            {
                Title = model.Title,
                About = model.About,
                BlogLink = model.BlogLink,
                CoverImagePath = coverImagePath,
                DisplayOrder = maxOrder + 1,
                IsVisible = model.IsVisible
            };

            try
            {
                _context.StudentClubs.Add(club);
                await _context.SaveChangesAsync();

                _logger.LogInformation("Student club {ClubId} '{Title}' created", club.Id, club.Title);

                TempData["Success"] = "Club added.";
                return RedirectToAction(nameof(Index));
            }
            catch (DbUpdateException ex)
            {
                _fileStorage.Delete(coverImagePath);
                _logger.LogError(ex, "Database error creating student club '{Title}'", model.Title);
                ModelState.AddModelError(string.Empty, "Unable to save the club. Please try again.");
                return View(model);
            }
            catch (Exception ex)
            {
                _fileStorage.Delete(coverImagePath);
                _logger.LogError(ex, "Unexpected error creating student club '{Title}'", model.Title);
                ModelState.AddModelError(string.Empty, "An unexpected error occurred. Please try again.");
                return View(model);
            }
        }

        public async Task<IActionResult> Edit(int id)
        {
            ViewData["Title"] = "Edit Club";
            var c = await _context.StudentClubs
                .Include(x => x.Images)
                .FirstOrDefaultAsync(x => x.Id == id);
            if (c == null) return NotFound();

            return View(new StudentClubEditVM
            {
                Id = c.Id,
                Title = c.Title,
                About = c.About,
                BlogLink = c.BlogLink,
                CoverImagePath = c.CoverImagePath,
                DisplayOrder = c.DisplayOrder,
                IsVisible = c.IsVisible,
                ExistingImages = c.Images
                    .Where(i => !i.IsDeleted)
                    .OrderBy(i => i.DisplayOrder)
                    .Select(i => new ClubImageVM
                    {
                        Id = i.Id,
                        ImagePath = i.ImagePath,
                        Caption = i.Caption,
                        DisplayOrder = i.DisplayOrder,
                        ClubId = i.ClubId
                    }).ToList()
            });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, StudentClubEditVM model)
        {
            ViewData["Title"] = "Edit Club";
            if (!ModelState.IsValid) return View(model);

            var c = await _context.StudentClubs.FindAsync(id);
            if (c == null) return NotFound();

            // Validate & save all files (cover image + any new carousel images)
            // before touching the database, so a rejected file never leaves a
            // half-saved club.
            var savedPaths = new List<string>();

            bool replacingCover = model.CoverImageFile is { Length: > 0 };
            string? newCoverPath = null;
            if (replacingCover)
            {
                var result = await _fileStorage.SaveAsync(model.CoverImageFile, ClubsFolder, FileCategory.Image);
                if (!result.Success)
                {
                    ModelState.AddModelError(nameof(model.CoverImageFile), result.ErrorMessage!);
                    return View(model);
                }
                newCoverPath = result.RelativePath;
                savedPaths.Add(newCoverPath!);
            }

            var newImages = new List<(string Path, string Caption)>();
            foreach (var file in Request.Form.Files.Where(f => f.Name == "NewImages" && f.Length > 0))
            {
                var result = await _fileStorage.SaveAsync(file, ClubsFolder, FileCategory.Image);
                if (!result.Success)
                {
                    foreach (var path in savedPaths) _fileStorage.Delete(path);
                    ModelState.AddModelError(string.Empty, $"'{file.FileName}': {result.ErrorMessage}");
                    return View(model);
                }
                savedPaths.Add(result.RelativePath!);
                newImages.Add((result.RelativePath!, Path.GetFileNameWithoutExtension(file.FileName)));
            }

            string? previousCoverPath = c.CoverImagePath;

            c.Title = model.Title;
            c.About = model.About;
            c.BlogLink = model.BlogLink;
            c.DisplayOrder = model.DisplayOrder;
            c.IsVisible = model.IsVisible;

            if (replacingCover)
                c.CoverImagePath = newCoverPath;

            int order = await _context.ClubImages
                .Where(i => i.ClubId == id && !i.IsDeleted)
                .Select(i => (int?)i.DisplayOrder).MaxAsync() ?? -1;

            try
            {
                foreach (var (path, caption) in newImages)
                {
                    _context.ClubImages.Add(new ClubImage
                    {
                        ClubId = id,
                        ImagePath = path,
                        Caption = caption,
                        DisplayOrder = ++order
                    });
                }

                await _context.SaveChangesAsync();

                // Old cover image removed only after the new state is safely persisted.
                if (replacingCover)
                    _fileStorage.Delete(previousCoverPath);

                _logger.LogInformation("Student club {ClubId} updated with {NewImageCount} new image(s)",
                    id, newImages.Count);

                TempData["Success"] = "Club updated.";
                return RedirectToAction(nameof(Index));
            }
            catch (DbUpdateException ex)
            {
                foreach (var path in savedPaths) _fileStorage.Delete(path);

                _logger.LogError(ex, "Database error updating student club {ClubId}", id);
                ModelState.AddModelError(string.Empty, "Unable to save the club. Please try again.");
                return View(model);
            }
            catch (Exception ex)
            {
                foreach (var path in savedPaths) _fileStorage.Delete(path);

                _logger.LogError(ex, "Unexpected error updating student club {ClubId}", id);
                ModelState.AddModelError(string.Empty, "An unexpected error occurred. Please try again.");
                return View(model);
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteImage(int imageId, int clubId)
        {
            var img = await _context.ClubImages.FindAsync(imageId);
            if (img == null) return NotFound();

            string? imagePath = img.ImagePath;
            img.IsDeleted = true;

            try
            {
                await _context.SaveChangesAsync();

                _fileStorage.Delete(imagePath);

                _logger.LogInformation("Image {ImageId} removed from club {ClubId}", imageId, clubId);
                TempData["Success"] = "Image removed.";
            }
            catch (DbUpdateException ex)
            {
                _logger.LogError(ex, "Database error removing image {ImageId} from club {ClubId}", imageId, clubId);
                TempData["Error"] = "Unable to remove the image. Please try again.";
            }

            return RedirectToAction(nameof(Edit), new { id = clubId });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ToggleVisible(int id)
        {
            var c = await _context.StudentClubs.FindAsync(id);
            if (c == null) return NotFound();

            c.IsVisible = !c.IsVisible;

            try
            {
                await _context.SaveChangesAsync();
                _logger.LogInformation("Student club {ClubId} visibility set to {IsVisible}", id, c.IsVisible);
            }
            catch (DbUpdateException ex)
            {
                _logger.LogError(ex, "Database error toggling visibility for student club {ClubId}", id);
                TempData["Error"] = "Unable to update visibility. Please try again.";
            }

            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int id)
        {
            var c = await _context.StudentClubs
                .Include(x => x.Images)
                .FirstOrDefaultAsync(x => x.Id == id);
            if (c == null) return NotFound();

            var pathsToDelete = new List<string?> { c.CoverImagePath };
            pathsToDelete.AddRange(c.Images.Select(i => i.ImagePath));

            c.IsDeleted = true;

            try
            {
                await _context.SaveChangesAsync();

                foreach (var path in pathsToDelete)
                    _fileStorage.Delete(path);

                _logger.LogInformation("Student club {ClubId} '{Title}' deleted with {ImageCount} image(s)",
                    id, c.Title, pathsToDelete.Count(p => !string.IsNullOrEmpty(p)));

                TempData["Success"] = "Club deleted.";
            }
            catch (DbUpdateException ex)
            {
                _logger.LogError(ex, "Database error deleting student club {ClubId}", id);
                TempData["Error"] = "Unable to delete the club. Please try again.";
            }

            return RedirectToAction(nameof(Index));
        }

        // ── MEMBERS ───────────────────────────────────────
        public async Task<IActionResult> Members(int id)
        {
            var club = await _context.StudentClubs.FindAsync(id);
            if (club == null) return NotFound();

            ViewBag.ClubId = id;
            ViewBag.ClubTitle = club.Title;
            ViewBag.Faculties = await _context.Faculties
                .Where(f => f.IsActive)
                .OrderBy(f => f.Name)
                .Select(f => new SelectListItem
                {
                    Value = f.FacultyId.ToString(),
                    Text = f.Name + (f.Department != null ? " (" + f.Department.Name + ")" : "")
                }).ToListAsync();

            var members = await _context.ClubMembers
                .Include(m => m.Faculty).ThenInclude(f => f!.Department)
                .Where(m => m.ClubId == id && !m.IsDeleted)
                .OrderBy(m => m.DisplayOrder)
                .ToListAsync();

            return View(members);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AddMember(ClubMemberVM model, IFormFile? Photo)
        {
            if (!ModelState.IsValid)
            {
                TempData["Error"] = string.Join(" ", ModelState.Values
                    .SelectMany(v => v.Errors).Select(e => e.ErrorMessage));
                return RedirectToAction(nameof(Members), new { id = model.ClubId });
            }

            if (model.MemberType == "Faculty" &&
                !await _context.Faculties.AnyAsync(f => f.FacultyId == model.FacultyId))
            {
                TempData["Error"] = "Please select a valid faculty member.";
                return RedirectToAction(nameof(Members), new { id = model.ClubId });
            }

            string? photoPath = null;
            if (model.MemberType != "Faculty" && Photo is { Length: > 0 })
            {
                var uploadResult = await _fileStorage.SaveAsync(Photo, ClubMembersFolder, FileCategory.Image);
                if (!uploadResult.Success)
                {
                    TempData["Error"] = uploadResult.ErrorMessage;
                    return RedirectToAction(nameof(Members), new { id = model.ClubId });
                }
                photoPath = uploadResult.RelativePath;
            }

            var member = new ClubMember
            {
                ClubId = model.ClubId,
                Position = model.Position,
                DisplayOrder = model.DisplayOrder
            };

            if (model.MemberType == "Faculty")
            {
                member.MemberType = ClubMemberType.Faculty;
                member.FacultyId = model.FacultyId;
                // Name / Department / ImagePath intentionally left null —
                // fetched from Faculty wherever this member is displayed.
            }
            else
            {
                member.MemberType = ClubMemberType.Student;
                member.Name = model.Name;
                member.Department = model.Department;
                member.Email = model.Email;
                member.ImagePath = photoPath;
            }

            try
            {
                _context.ClubMembers.Add(member);
                await _context.SaveChangesAsync();

                _logger.LogInformation("Member {MemberId} added to club {ClubId}", member.Id, model.ClubId);
                TempData["Success"] = "Member added.";
            }
            catch (DbUpdateException ex)
            {
                _fileStorage.Delete(photoPath);
                _logger.LogError(ex, "Database error adding member to club {ClubId}", model.ClubId);
                TempData["Error"] = "Unable to add the member. Please try again.";
            }

            return RedirectToAction(nameof(Members), new { id = model.ClubId });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteMember(int id, int clubId)
        {
            var m = await _context.ClubMembers.FindAsync(id);
            if (m == null) return NotFound();

            string? imagePath = m.ImagePath;
            m.IsDeleted = true;

            try
            {
                await _context.SaveChangesAsync();

                // Only Student-type members have an ImagePath; Faculty members
                // have none (photo comes from the linked Faculty record), so
                // this is a safe no-op for those.
                _fileStorage.Delete(imagePath);

                _logger.LogInformation("Member {MemberId} removed from club {ClubId}", id, clubId);
                TempData["Success"] = "Member removed.";
            }
            catch (DbUpdateException ex)
            {
                _logger.LogError(ex, "Database error deleting member {MemberId}", id);
                TempData["Error"] = "Unable to remove the member. Please try again.";
            }

            return RedirectToAction(nameof(Members), new { id = clubId });
        }
    }
}