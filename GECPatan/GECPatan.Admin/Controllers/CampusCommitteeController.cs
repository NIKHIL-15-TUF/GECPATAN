using GECPatan.Core.Data;
using GECPatan.Core.Models.Domain;
using GECPatan.Admin.Models.ViewModels;
using GECPatan.Core.Services.FileStorage;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;

namespace GECPatan.Admin.Controllers
{
    [Authorize(Roles = "SuperAdmin,HOD,ContentEditor,Principal")]
    public class CampusCommitteeController : Controller
    {
        private const string UploadFolder = "committees";
        private const string SuperAdminRole = "SuperAdmin";

        private readonly ApplicationDbContext _context;
        private readonly IFileStorageService _fileStorage;
        private readonly ILogger<CampusCommitteeController> _logger;

        public CampusCommitteeController(
            ApplicationDbContext context,
            IFileStorageService fileStorage,
            ILogger<CampusCommitteeController> logger)
        {
            _context = context;
            _fileStorage = fileStorage;
            _logger = logger;
        }

        // ── INDEX ─────────────────────────────────────────
        public async Task<IActionResult> Index()
        {
            ViewData["Title"] = "Campus Committees";
            var committees = await _context.CampusCommittees
                .AsNoTracking()
                .Where(c => !c.IsDeleted)
                .Include(c => c.Members)
                .OrderBy(c => c.DisplayOrder)
                .Select(c => new CommitteeListVM
                {
                    Id = c.Id,
                    Title = c.Title,
                    Tagline = c.Tagline,
                    TitleImagePath = c.TitleImagePath,
                    IsActive = c.IsActive,
                    DisplayOrder = c.DisplayOrder,
                    MemberCount = c.Members.Count(m => !m.IsDeleted)
                })
                .ToListAsync();
            return View(committees);
        }

        // ── CREATE GET ────────────────────────────────────
        [Authorize(Roles = SuperAdminRole)]
        public IActionResult Create()
        {
            ViewData["Title"] = "Add Committee";
            return View(new CommitteeCreateVM());
        }

        // ── CREATE POST ───────────────────────────────────
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = SuperAdminRole)]
        public async Task<IActionResult> Create(CommitteeCreateVM model, IFormFile? TitleImage, IFormFile? MeasureImage)
        {
            ViewData["Title"] = "Add Committee";
            if (!ModelState.IsValid) return View(model);

            string? titleImagePath = null;
            string? measureImagePath = null;

            if (TitleImage != null && TitleImage.Length > 0)
            {
                var result = await _fileStorage.SaveAsync(TitleImage, UploadFolder, FileCategory.Image);
                if (!result.Success)
                {
                    ModelState.AddModelError(nameof(TitleImage), result.ErrorMessage!);
                    return View(model);
                }
                titleImagePath = result.RelativePath;
            }

            if (MeasureImage != null && MeasureImage.Length > 0)
            {
                var result = await _fileStorage.SaveAsync(MeasureImage, UploadFolder, FileCategory.Image);
                if (!result.Success)
                {
                    ModelState.AddModelError(nameof(MeasureImage), result.ErrorMessage!);
                    CleanupOrphanFile(titleImagePath);
                    return View(model);
                }
                measureImagePath = result.RelativePath;
            }

            try
            {
                var committee = new CampusCommittee
                {
                    Title = model.Title,
                    About = model.About,
                    Tagline = model.Tagline,
                    Measures = model.Measures,
                    Message = model.Message,
                    BlogLink = model.BlogLink,
                    Link = model.Link,
                    NationalTaskForce = model.NationalTaskForce,
                    ShowDocument = model.ShowDocument,
                    TableView = model.TableView,
                    DisplayOrder = model.DisplayOrder,
                    IsActive = true,
                    TabAbout = model.TabAbout,
                    TabVisionMission = model.TabVisionMission,
                    TabObjectives = model.TabObjectives,
                    TabMembers = model.TabMembers,
                    TabActivities = model.TabActivities,
                    TabDocuments = model.TabDocuments,
                    TabLink = model.TabLink,
                    TitleImagePath = titleImagePath,
                    MeasureImagePath = measureImagePath
                };

                _context.CampusCommittees.Add(committee);
                await _context.SaveChangesAsync();

                _logger.LogInformation("Committee {Id} '{Title}' created by {User}", committee.Id, committee.Title, User.Identity?.Name);
                TempData["Success"] = $"Committee '{committee.Title}' created.";
                return RedirectToAction(nameof(Index));
            }
            catch (DbUpdateException ex)
            {
                _logger.LogError(ex, "Database error while creating committee '{Title}'", model.Title);
                CleanupOrphanFile(titleImagePath);
                CleanupOrphanFile(measureImagePath);
                ModelState.AddModelError(string.Empty, "Could not save the committee due to a database error. Please try again.");
                return View(model);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error while creating committee '{Title}'", model.Title);
                CleanupOrphanFile(titleImagePath);
                CleanupOrphanFile(measureImagePath);
                ModelState.AddModelError(string.Empty, "An unexpected error occurred while creating the committee. Please try again.");
                return View(model);
            }
        }

        // ── EDIT GET ──────────────────────────────────────
        public async Task<IActionResult> Edit(int id)
        {
            ViewData["Title"] = "Edit Committee";
            var c = await _context.CampusCommittees
                .Include(x => x.Visions)
                .Include(x => x.Missions)
                .Include(x => x.Objectives)
                .Include(x => x.SubObjectives)
                .FirstOrDefaultAsync(x => x.Id == id && !x.IsDeleted);
            if (c == null) return NotFound();

            return View(new CommitteeEditVM
            {
                Id = c.Id,
                Title = c.Title,
                About = c.About,
                Tagline = c.Tagline,
                Measures = c.Measures,
                Message = c.Message,
                BlogLink = c.BlogLink,
                Link = c.Link,
                NationalTaskForce = c.NationalTaskForce,
                ShowDocument = c.ShowDocument,
                TableView = c.TableView,
                DisplayOrder = c.DisplayOrder,
                TabAbout = c.TabAbout,
                TabVisionMission = c.TabVisionMission,
                TabObjectives = c.TabObjectives,
                TabMembers = c.TabMembers,
                TabActivities = c.TabActivities,
                TabDocuments = c.TabDocuments,
                TabLink = c.TabLink,
                ExistingTitleImagePath = c.TitleImagePath,
                ExistingMeasureImagePath = c.MeasureImagePath,
                ExistingSubObjImagePath = c.SubObjImagePath,
                ExistingBulletPointsImagePath = c.BulletPointsImagePath,
                ExistingPageFlyerPath = c.PageFlyerPath,
                VisionItems = c.Visions.OrderBy(v => v.DisplayOrder).Select(v => v.VisionText).ToList(),
                MissionItems = c.Missions.OrderBy(m => m.DisplayOrder).Select(m => m.MissionText).ToList(),
                ObjectiveItems = c.Objectives.OrderBy(o => o.DisplayOrder).Select(o => o.ObjectiveText).ToList(),
                SubObjectiveItems = c.SubObjectives.OrderBy(s => s.DisplayOrder).Select(s => s.SubObjectiveText).ToList()
            });
        }

        // ── EDIT POST ─────────────────────────────────────
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, CommitteeEditVM model,
            IFormFile? TitleImage, IFormFile? MeasureImage,
            IFormFile? SubObjImage, IFormFile? BulletPointsImage, IFormFile? PageFlyer,
            string? VisionItems, string? MissionItems,
            string? ObjectiveItems, string? SubObjectiveItems)
        {
            ViewData["Title"] = "Edit Committee";
            if (id != model.Id) return NotFound();
            if (!ModelState.IsValid) return View(model);

            var c = await _context.CampusCommittees
                .Include(x => x.Visions)
                .Include(x => x.Missions)
                .Include(x => x.Objectives)
                .Include(x => x.SubObjectives)
                .FirstOrDefaultAsync(x => x.Id == id && !x.IsDeleted);
            if (c == null) return NotFound();

            // Each entry: (posted file, current path on the entity, field name for
            // validation errors, category). Uploaded up front, before any DB
            // writes, and tracked so a failed save can clean them back up.
            var uploads = new (IFormFile? File, string? CurrentPath, string FieldName, FileCategory Category)[]
            {
                (TitleImage, c.TitleImagePath, nameof(TitleImage), FileCategory.Image),
                (MeasureImage, c.MeasureImagePath, nameof(MeasureImage), FileCategory.Image),
                (SubObjImage, c.SubObjImagePath, nameof(SubObjImage), FileCategory.Image),
                (BulletPointsImage, c.BulletPointsImagePath, nameof(BulletPointsImage), FileCategory.Image),
                (PageFlyer, c.PageFlyerPath, nameof(PageFlyer), FileCategory.Document),
            };

            var newlySavedFiles = new List<string>();
            var oldFilesToRemove = new List<string>();
            string? newTitleImagePath = null, newMeasureImagePath = null,
                    newSubObjImagePath = null, newBulletPointsImagePath = null, newPageFlyerPath = null;

            foreach (var (file, currentPath, fieldName, category) in uploads)
            {
                if (file == null || file.Length == 0) continue;

                var result = await _fileStorage.SaveAsync(file, UploadFolder, category);
                if (!result.Success)
                {
                    ModelState.AddModelError(fieldName, result.ErrorMessage!);
                    CleanupOrphanFiles(newlySavedFiles);
                    return View(model);
                }

                newlySavedFiles.Add(result.RelativePath!);
                if (currentPath != null) oldFilesToRemove.Add(currentPath);

                switch (fieldName)
                {
                    case nameof(TitleImage): newTitleImagePath = result.RelativePath; break;
                    case nameof(MeasureImage): newMeasureImagePath = result.RelativePath; break;
                    case nameof(SubObjImage): newSubObjImagePath = result.RelativePath; break;
                    case nameof(BulletPointsImage): newBulletPointsImagePath = result.RelativePath; break;
                    case nameof(PageFlyer): newPageFlyerPath = result.RelativePath; break;
                }
            }

            c.Title = model.Title;
            c.About = model.About;
            c.Tagline = model.Tagline;
            c.Measures = model.Measures;
            c.Message = model.Message;
            c.BlogLink = model.BlogLink;
            c.Link = model.Link;
            c.NationalTaskForce = model.NationalTaskForce;
            c.ShowDocument = model.ShowDocument;
            c.TableView = model.TableView;
            c.DisplayOrder = model.DisplayOrder;
            c.TabAbout = model.TabAbout;
            c.TabVisionMission = model.TabVisionMission;
            c.TabObjectives = model.TabObjectives;
            c.TabMembers = model.TabMembers;
            c.TabActivities = model.TabActivities;
            c.TabDocuments = model.TabDocuments;
            c.TabLink = model.TabLink;

            if (newTitleImagePath != null) c.TitleImagePath = newTitleImagePath;
            if (newMeasureImagePath != null) c.MeasureImagePath = newMeasureImagePath;
            if (newSubObjImagePath != null) c.SubObjImagePath = newSubObjImagePath;
            if (newBulletPointsImagePath != null) c.BulletPointsImagePath = newBulletPointsImagePath;
            if (newPageFlyerPath != null) c.PageFlyerPath = newPageFlyerPath;

            _context.CommitteeVisions.RemoveRange(c.Visions);
            ReplaceTextList(_context.CommitteeVisions, VisionItems,
                (text, order) => new CommitteeVision { CommitteeId = id, VisionText = text, DisplayOrder = order });

            _context.CommitteeMissions.RemoveRange(c.Missions);
            ReplaceTextList(_context.CommitteeMissions, MissionItems,
                (text, order) => new CommitteeMission { CommitteeId = id, MissionText = text, DisplayOrder = order });

            _context.CommitteeObjectives.RemoveRange(c.Objectives);
            ReplaceTextList(_context.CommitteeObjectives, ObjectiveItems,
                (text, order) => new CommitteeObjective { CommitteeId = id, ObjectiveText = text, DisplayOrder = order });

            _context.CommitteeSubObjectives.RemoveRange(c.SubObjectives);
            ReplaceTextList(_context.CommitteeSubObjectives, SubObjectiveItems,
                (text, order) => new CommitteeSubObjective { CommitteeId = id, SubObjectiveText = text, DisplayOrder = order });

            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateException ex)
            {
                _logger.LogError(ex, "Database error while editing committee {Id}", id);
                CleanupOrphanFiles(newlySavedFiles);
                ModelState.AddModelError(string.Empty, "Could not save the committee due to a database error. Please try again.");
                return View(model);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error while editing committee {Id}", id);
                CleanupOrphanFiles(newlySavedFiles);
                ModelState.AddModelError(string.Empty, "An unexpected error occurred while saving the committee. Please try again.");
                return View(model);
            }

            // Only remove replaced files once the new state is safely
            // persisted, so a failed save never leaves the committee with a
            // missing image or document.
            foreach (var path in oldFilesToRemove)
                TryDeleteFile(path, "replaced committee file");

            _logger.LogInformation("Committee {Id} '{Title}' edited by {User}", c.Id, c.Title, User.Identity?.Name);
            TempData["Success"] = $"Committee '{c.Title}' updated.";
            return RedirectToAction(nameof(Index));
        }

        // ── TOGGLE ACTIVE ─────────────────────────────────
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = SuperAdminRole)]
        public async Task<IActionResult> ToggleActive(int id)
        {
            var c = await _context.CampusCommittees.FindAsync(id);
            if (c == null) return NotFound();
            c.IsActive = !c.IsActive;

            try
            {
                await _context.SaveChangesAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to toggle active state for committee {Id}", id);
                TempData["Error"] = "Could not update the committee status. Please try again.";
                return RedirectToAction(nameof(Index));
            }

            TempData["Success"] = $"'{c.Title}' " + (c.IsActive ? "activated" : "deactivated") + ".";
            return RedirectToAction(nameof(Index));
        }

        // ── DELETE ────────────────────────────────────────
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = SuperAdminRole)]
        public async Task<IActionResult> Delete(int id)
        {
            var c = await _context.CampusCommittees.FindAsync(id);
            if (c == null) return NotFound();
            c.IsDeleted = true;
            c.IsActive = false;

            try
            {
                await _context.SaveChangesAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to delete committee {Id}", id);
                TempData["Error"] = "Could not delete the committee. Please try again.";
                return RedirectToAction(nameof(Index));
            }

            _logger.LogInformation("Committee {Id} '{Title}' deleted by {User}", c.Id, c.Title, User.Identity?.Name);
            TempData["Success"] = $"Committee '{c.Title}' deleted.";
            return RedirectToAction(nameof(Index));
        }

        // ── MEMBERS ───────────────────────────────────────
        public async Task<IActionResult> Members(int id)
        {
            ViewData["Title"] = "Committee Members";
            var c = await _context.CampusCommittees.FirstOrDefaultAsync(x => x.Id == id && !x.IsDeleted);
            if (c == null) return NotFound();

            ViewBag.CommitteeId = id;
            ViewBag.CommitteeTitle = c.Title;

            var members = await _context.CommitteeMembers
                .Where(m => m.CommitteeId == id && !m.IsDeleted)
                .OrderBy(m => m.DisplayOrder)
                .ToListAsync();

            ViewBag.FacultyList = await _context.Faculties
                .Where(f => f.IsActive)
                .OrderBy(f => f.Name)
                .Select(f => new SelectListItem
                {
                    Value = f.FacultyId.ToString(),
                    Text = $"{f.Name} ({f.Designation})"
                })
                .ToListAsync();

            return View(members);
        }

        // Get faculty details for AJAX auto-fill
        [HttpGet]
        public async Task<IActionResult> GetFacultyDetails(int facultyId)
        {
            var f = await _context.Faculties
                .Include(x => x.Department)
                .Include(x => x.PersonalDetail)
                .FirstOrDefaultAsync(x => x.FacultyId == facultyId);

            if (f == null) return NotFound();

            return Json(new
            {
                name = f.Name,
                department = f.Department?.Name ?? "",
                email = f.PersonalDetail?.Email ?? "",
                contact = f.PersonalDetail?.Contact ?? ""
            });
        }

        // ── ADD MEMBER ────────────────────────────────────
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AddMember(CommitteeMemberVM model, IFormFile? MemberPhoto)
        {
            if (!ModelState.IsValid)
            {
                TempData["Error"] = "Could not add the member — please check the form and try again.";
                return RedirectToAction(nameof(Members), new { id = model.CommitteeId });
            }

            string? photoPath = null;
            if (MemberPhoto != null && MemberPhoto.Length > 0)
            {
                var result = await _fileStorage.SaveAsync(MemberPhoto, UploadFolder, FileCategory.Image);
                if (!result.Success)
                {
                    TempData["Error"] = result.ErrorMessage;
                    return RedirectToAction(nameof(Members), new { id = model.CommitteeId });
                }
                photoPath = result.RelativePath;
            }
            else if (model.FacultyId.HasValue)
            {
                // Use the faculty's existing photo if no new one was uploaded.
                var f = await _context.Faculties.FindAsync(model.FacultyId.Value);
                if (f?.ImagePath != null)
                    photoPath = f.ImagePath;
            }

            try
            {
                var member = new CommitteeMember
                {
                    CommitteeId = model.CommitteeId,
                    Name = model.Name,
                    Position = model.Position,
                    Email = model.Email,
                    Contact = model.Contact,
                    Department = model.Department,
                    DisplayOrder = model.DisplayOrder,
                    ImagePath = photoPath
                };

                _context.CommitteeMembers.Add(member);
                await _context.SaveChangesAsync();

                _logger.LogInformation("Member '{Name}' added to committee {CommitteeId} by {User}", member.Name, model.CommitteeId, User.Identity?.Name);
                TempData["Success"] = "Member added.";
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to add member to committee {CommitteeId}", model.CommitteeId);
                // Only clean up a photo we actually saved ourselves — a
                // reused faculty photo must never be deleted.
                if (MemberPhoto != null) CleanupOrphanFile(photoPath);
                TempData["Error"] = "Could not add the member due to an error. Please try again.";
            }

            return RedirectToAction(nameof(Members), new { id = model.CommitteeId });
        }

        // ── DELETE MEMBER ─────────────────────────────────
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteMember(int id, int committeeId)
        {
            var m = await _context.CommitteeMembers.FindAsync(id);
            if (m == null || m.CommitteeId != committeeId) return NotFound();

            m.IsDeleted = true;

            try
            {
                await _context.SaveChangesAsync();
                TempData["Success"] = "Member removed.";
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to remove member {MemberId} from committee {CommitteeId}", id, committeeId);
                TempData["Error"] = "Could not remove the member. Please try again.";
            }

            return RedirectToAction(nameof(Members), new { id = committeeId });
        }

        // ── HELPERS ───────────────────────────────────────

        /// <summary>
        /// Replaces a committee's child text-list collection (Vision, Mission,
        /// Objective, SubObjective items) with the newline-separated values
        /// submitted by the form, assigning a real, increasing DisplayOrder to
        /// each — the previous SaveList() helper took an order counter but
        /// never actually passed it to the created entity, so every item was
        /// silently saved with DisplayOrder = 0.
        /// </summary>
        private void ReplaceTextList<T>(DbSet<T> set, string? rawItems, Func<string, int, T> factory) where T : class
        {
            if (string.IsNullOrEmpty(rawItems)) return;

            var items = rawItems.Split('\n', StringSplitOptions.RemoveEmptyEntries);
            int order = 0;
            foreach (var raw in items)
            {
                var text = raw.Trim();
                if (text.Length == 0) continue;
                set.Add(factory(text, order++));
            }
        }

        /// <summary>
        /// Deletes a single file and never throws — a failed delete here
        /// should never take down the request; it just gets logged so it can
        /// be cleaned up manually.
        /// </summary>
        private void TryDeleteFile(string? path, string context)
        {
            if (string.IsNullOrEmpty(path)) return;

            try
            {
                if (!_fileStorage.Delete(path))
                    _logger.LogWarning("File delete returned false for {Path} ({Context})", path, context);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to delete file {Path} ({Context})", path, context);
            }
        }

        private void CleanupOrphanFile(string? path) => TryDeleteFile(path, "orphan cleanup");

        private void CleanupOrphanFiles(IEnumerable<string> paths)
        {
            foreach (var path in paths)
                TryDeleteFile(path, "orphan cleanup");
        }
    }
}