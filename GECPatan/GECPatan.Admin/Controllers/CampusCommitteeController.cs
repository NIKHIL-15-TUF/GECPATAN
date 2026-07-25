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
    public class CampusCommitteeController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly IWebHostEnvironment _env;

        public CampusCommitteeController(ApplicationDbContext context, IWebHostEnvironment env)
        {
            _context = context;
            _env = env;
        }

        public async Task<IActionResult> Index()
        {
            ViewData["Title"] = "Campus Committees";
            var committees = await _context.CampusCommittees
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
                    MemberCount = c.Members.Count
                })
                .ToListAsync();
            return View(committees);
        }

        [Authorize(Roles = "SuperAdmin")]
        public IActionResult Create()
        {
            ViewData["Title"] = "Add Committee";
            return View(new CommitteeCreateVM());
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "SuperAdmin")]
        public async Task<IActionResult> Create(CommitteeCreateVM model, IFormFile? TitleImage, IFormFile? MeasureImage)
        {
            ViewData["Title"] = "Add Committee";
            if (!ModelState.IsValid) return View(model);

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
                TabLink = model.TabLink
            };

            if (TitleImage != null && TitleImage.Length > 0)
                committee.TitleImagePath = await SaveFileAsync(TitleImage, "committees");

            if (MeasureImage != null && MeasureImage.Length > 0)    
                committee.MeasureImagePath = await SaveFileAsync(MeasureImage, "committees");

            _context.CampusCommittees.Add(committee);
            await _context.SaveChangesAsync();
            TempData["Success"] = $"Committee '{committee.Title}' created.";
            return RedirectToAction(nameof(Index));
        }

        public async Task<IActionResult> Edit(int id)
        {
            ViewData["Title"] = "Edit Committee";
            var c = await _context.CampusCommittees
                .Include(x => x.Visions)
                .Include(x => x.Missions)
                .Include(x => x.Objectives)
                .Include(x => x.SubObjectives)
                .FirstOrDefaultAsync(x => x.Id == id);
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
                .FirstOrDefaultAsync(x => x.Id == id);
            if (c == null) return NotFound();

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

            if (TitleImage != null && TitleImage.Length > 0) { DeleteFile(c.TitleImagePath); c.TitleImagePath = await SaveFileAsync(TitleImage, "committees"); }
            if (MeasureImage != null && MeasureImage.Length > 0) { DeleteFile(c.MeasureImagePath); c.MeasureImagePath = await SaveFileAsync(MeasureImage, "committees"); }
            if (SubObjImage != null && SubObjImage.Length > 0) { DeleteFile(c.SubObjImagePath); c.SubObjImagePath = await SaveFileAsync(SubObjImage, "committees"); }
            if (BulletPointsImage != null && BulletPointsImage.Length > 0) { DeleteFile(c.BulletPointsImagePath); c.BulletPointsImagePath = await SaveFileAsync(BulletPointsImage, "committees"); }
            if (PageFlyer != null && PageFlyer.Length > 0) { DeleteFile(c.PageFlyerPath); c.PageFlyerPath = await SaveFileAsync(PageFlyer, "committees"); }

            // Vision
            _context.CommitteeVisions.RemoveRange(c.Visions);
            if (!string.IsNullOrEmpty(VisionItems))
                SaveList(VisionItems, i => _context.CommitteeVisions.Add(new CommitteeVision { CommitteeId = id, VisionText = i.Trim(), DisplayOrder = 0 }));

            // Mission
            _context.CommitteeMissions.RemoveRange(c.Missions);
            if (!string.IsNullOrEmpty(MissionItems))
                SaveList(MissionItems, i => _context.CommitteeMissions.Add(new CommitteeMission { CommitteeId = id, MissionText = i.Trim(), DisplayOrder = 0 }));

            // Objectives
            _context.CommitteeObjectives.RemoveRange(c.Objectives);
            if (!string.IsNullOrEmpty(ObjectiveItems))
                SaveList(ObjectiveItems, i => _context.CommitteeObjectives.Add(new CommitteeObjective { CommitteeId = id, ObjectiveText = i.Trim(), DisplayOrder = 0 }));

            // SubObjectives
            _context.CommitteeSubObjectives.RemoveRange(c.SubObjectives);
            if (!string.IsNullOrEmpty(SubObjectiveItems))
                SaveList(SubObjectiveItems, i => _context.CommitteeSubObjectives.Add(new CommitteeSubObjective { CommitteeId = id, SubObjectiveText = i.Trim(), DisplayOrder = 0 }));

            await _context.SaveChangesAsync();
            TempData["Success"] = $"Committee '{c.Title}' updated.";
            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        [Authorize(Roles = "SuperAdmin")]
        public async Task<IActionResult> ToggleActive(int id)
        {
            var c = await _context.CampusCommittees.FindAsync(id);
            if (c == null) return NotFound();
            c.IsActive = !c.IsActive;
            await _context.SaveChangesAsync();
            TempData["Success"] = $"'{c.Title}' " + (c.IsActive ? "activated" : "deactivated") + ".";
            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        [Authorize(Roles = "SuperAdmin")]
        public async Task<IActionResult> Delete(int id)
        {
            var c = await _context.CampusCommittees.FindAsync(id);
            if (c == null) return NotFound();
            c.IsDeleted = true; c.IsActive = false;
            await _context.SaveChangesAsync();
            TempData["Success"] = $"Committee '{c.Title}' deleted.";
            return RedirectToAction(nameof(Index));
        }

        // ── MEMBERS ───────────────────────────────────────
        public async Task<IActionResult> Members(int id)
        {
            ViewData["Title"] = "Committee Members";
            var c = await _context.CampusCommittees.FindAsync(id);
            if (c == null) return NotFound();

            ViewBag.CommitteeId = id;
            ViewBag.CommitteeTitle = c.Title;

            var members = await _context.CommitteeMembers
                .Where(m => m.CommitteeId == id)
                .OrderBy(m => m.DisplayOrder)
                .ToListAsync();

            // Faculty dropdown for adding members
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

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AddMember(CommitteeMemberVM model, IFormFile? MemberPhoto)
        {
            if (ModelState.IsValid)
            {
                var member = new CommitteeMember
                {
                    CommitteeId = model.CommitteeId,
                    Name = model.Name,
                    Position = model.Position,
                    Email = model.Email,
                    Contact = model.Contact,
                    Department = model.Department,
                    DisplayOrder = model.DisplayOrder
                };

                if (MemberPhoto != null && MemberPhoto.Length > 0)
                    member.ImagePath = await SaveFileAsync(MemberPhoto, "committees");
                else if (model.FacultyId.HasValue)
                {
                    // Use faculty photo if available
                    var f = await _context.Faculties.FindAsync(model.FacultyId.Value);
                    if (f?.ImagePath != null)
                        member.ImagePath = f.ImagePath;
                }

                _context.CommitteeMembers.Add(member);
                await _context.SaveChangesAsync();
                TempData["Success"] = "Member added.";
            }
            return RedirectToAction(nameof(Members), new { id = model.CommitteeId });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteMember(int id, int committeeId)
        {
            var m = await _context.CommitteeMembers.FindAsync(id);
            if (m != null) { m.IsDeleted = true; await _context.SaveChangesAsync(); TempData["Success"] = "Member removed."; }
            return RedirectToAction(nameof(Members), new { id = committeeId });
        }

        // ── HELPERS ───────────────────────────────────────
        private void SaveList(string raw, Action<string> addItem)
        {
            int order = 0;
            foreach (var line in raw.Split('\n', StringSplitOptions.RemoveEmptyEntries))
                if (!string.IsNullOrWhiteSpace(line))
                    addItem(line);
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