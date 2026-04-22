using GECPatan.Admin.Data;
using GECPatan.Admin.Models.Domain;
using GECPatan.Admin.Models.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace GECPatan.Admin.Controllers
{
    [Authorize(Roles = "SuperAdmin,ContentEditor")]
    public class FacilityController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly IWebHostEnvironment _env;

        public FacilityController(ApplicationDbContext context, IWebHostEnvironment env)
        {
            _context = context;
            _env = env;
        }

        // ── INDEX ─────────────────────────────────────────
        public async Task<IActionResult> Index()
        {
            ViewData["Title"] = "Facilities";
            var facilities = await _context.Facilities
                .Include(f => f.Members)
                .OrderBy(f => f.DisplayOrder)
                .ToListAsync();

            var list = new List<FacilityListVM>();
            foreach (var f in facilities)
            {
                var sectionCount = await _context.DynamicSections
                    .CountAsync(s => s.PageType == PageType.Facility && s.PageId == f.Id);

                list.Add(new FacilityListVM
                {
                    Id = f.Id,
                    Title = f.Title,
                    Tagline = f.Tagline,
                    TitleImagePath = f.TitleImagePath,
                    IsActive = f.IsActive,
                    DisplayOrder = f.DisplayOrder,
                    MemberCount = f.Members.Count,
                    SectionCount = sectionCount
                });
            }

            return View(list);
        }

        // ── CREATE ────────────────────────────────────────
        [Authorize(Roles = "SuperAdmin")]
        public IActionResult Create()
        {
            ViewData["Title"] = "Add Facility";
            return View(new FacilityCreateVM());
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "SuperAdmin")]
        public async Task<IActionResult> Create(FacilityCreateVM model, IFormFile? TitleImage)
        {
            ViewData["Title"] = "Add Facility";
            if (!ModelState.IsValid) return View(model);

            int maxOrder = await _context.Facilities
                .Select(f => (int?)f.DisplayOrder).MaxAsync() ?? -1;

            var facility = new Facility
            {
                Title = model.Title,
                Tagline = model.Tagline,
                About = model.About,
                DisplayOrder = maxOrder + 1,
                IsActive = true
            };

            if (TitleImage != null && TitleImage.Length > 0)
                facility.TitleImagePath = await SaveFileAsync(TitleImage, "facilities");

            _context.Facilities.Add(facility);
            await _context.SaveChangesAsync();

            TempData["Success"] = $"Facility '{facility.Title}' created.";
            return RedirectToAction(nameof(Index));
        }

        // ── EDIT ──────────────────────────────────────────
        public async Task<IActionResult> Edit(int id)
        {
            ViewData["Title"] = "Edit Facility";
            var f = await _context.Facilities
                .Include(x => x.Members)
                .Include(x => x.Visions)
                .Include(x => x.Missions)
                .FirstOrDefaultAsync(x => x.Id == id);

            if (f == null) return NotFound();

            var sectionCount = await _context.DynamicSections
                .CountAsync(s => s.PageType == PageType.Facility && s.PageId == id);

            return View(new FacilityEditVM
            {
                Id = f.Id,
                Title = f.Title,
                Tagline = f.Tagline,
                About = f.About,
                DisplayOrder = f.DisplayOrder,
                ExistingTitleImagePath = f.TitleImagePath,
                MemberCount = f.Members.Count,
                SectionCount = sectionCount,
                VisionItems = f.Visions.OrderBy(v => v.DisplayOrder).Select(v => v.VisionText).ToList(),
                MissionItems = f.Missions.OrderBy(m => m.DisplayOrder).Select(m => m.MissionText).ToList()
            });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, FacilityEditVM model,
            IFormFile? TitleImage, string? VisionItems, string? MissionItems)
        {
            ViewData["Title"] = "Edit Facility";
            if (!ModelState.IsValid) return View(model);

            var f = await _context.Facilities
                .Include(x => x.Visions)
                .Include(x => x.Missions)
                .FirstOrDefaultAsync(x => x.Id == id);
            if (f == null) return NotFound();

            f.Title = model.Title;
            f.Tagline = model.Tagline;
            f.About = model.About;
            f.DisplayOrder = model.DisplayOrder;

            if (TitleImage != null && TitleImage.Length > 0)
            {
                DeleteFile(f.TitleImagePath);
                f.TitleImagePath = await SaveFileAsync(TitleImage, "facilities");
            }

            // Vision
            _context.FacilityVisions.RemoveRange(f.Visions);
            if (!string.IsNullOrEmpty(VisionItems))
            {
                int i = 0;
                foreach (var line in VisionItems.Split('\n', StringSplitOptions.RemoveEmptyEntries))
                    if (!string.IsNullOrWhiteSpace(line))
                        _context.FacilityVisions.Add(new FacilityVision
                        {
                            FacilityId = id,
                            VisionText = line.Trim(),
                            DisplayOrder = i++
                        });
            }

            // Mission
            _context.FacilityMissions.RemoveRange(f.Missions);
            if (!string.IsNullOrEmpty(MissionItems))
            {
                int i = 0;
                foreach (var line in MissionItems.Split('\n', StringSplitOptions.RemoveEmptyEntries))
                    if (!string.IsNullOrWhiteSpace(line))
                        _context.FacilityMissions.Add(new FacilityMission
                        {
                            FacilityId = id,
                            MissionText = line.Trim(),
                            DisplayOrder = i++
                        });
            }

            await _context.SaveChangesAsync();
            TempData["Success"] = $"Facility '{f.Title}' updated.";
            return RedirectToAction(nameof(Index));
        }

        // ── TOGGLE / DELETE ───────────────────────────────
        [HttpPost]
        [Authorize(Roles = "SuperAdmin")]
        public async Task<IActionResult> ToggleActive(int id)
        {
            var f = await _context.Facilities.FindAsync(id);
            if (f == null) return NotFound();
            f.IsActive = !f.IsActive;
            await _context.SaveChangesAsync();
            TempData["Success"] = $"'{f.Title}' " + (f.IsActive ? "activated" : "deactivated") + ".";
            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        [Authorize(Roles = "SuperAdmin")]
        public async Task<IActionResult> Delete(int id)
        {
            var f = await _context.Facilities.FindAsync(id);
            if (f == null) return NotFound();
            DeleteFile(f.TitleImagePath);
            f.IsDeleted = true;
            f.IsActive = false;
            await _context.SaveChangesAsync();
            TempData["Success"] = $"Facility '{f.Title}' deleted.";
            return RedirectToAction(nameof(Index));
        }

        // ── REORDER ───────────────────────────────────────
        [HttpPost]
        [Authorize(Roles = "SuperAdmin")]
        public async Task<IActionResult> Reorder(int id, string direction)
        {
            var f = await _context.Facilities.FindAsync(id);
            if (f == null) return NotFound();
            if (direction == "up")
            {
                var above = await _context.Facilities
                    .Where(x => x.DisplayOrder == f.DisplayOrder - 1).FirstOrDefaultAsync();
                if (above != null) { above.DisplayOrder++; f.DisplayOrder--; }
            }
            else
            {
                var below = await _context.Facilities
                    .Where(x => x.DisplayOrder == f.DisplayOrder + 1).FirstOrDefaultAsync();
                if (below != null) { below.DisplayOrder--; f.DisplayOrder++; }
            }
            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        // ── MEMBERS ───────────────────────────────────────
        public async Task<IActionResult> Members(int id)
        {
            ViewData["Title"] = "Facility Members";
            var facility = await _context.Facilities.FindAsync(id);
            if (facility == null) return NotFound();

            ViewBag.FacilityId = id;
            ViewBag.FacilityTitle = facility.Title;

            var members = await _context.FacilityMembers
                .Where(m => m.FacilityId == id)
                .OrderBy(m => m.DisplayOrder)
                .ToListAsync();

            return View(members);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AddMember(FacilityMemberVM model, IFormFile? Photo)
        {
            if (ModelState.IsValid)
            {
                var member = new FacilityMember
                {
                    FacilityId = model.FacilityId,
                    Name = model.Name,
                    Position = model.Position,
                    Department = model.Department,
                    Email = model.Email,
                    Contact = model.Contact,
                    DisplayOrder = model.DisplayOrder
                };

                if (Photo != null && Photo.Length > 0)
                    member.ImagePath = await SaveFileAsync(Photo, "facilities");

                _context.FacilityMembers.Add(member);
                await _context.SaveChangesAsync();
                TempData["Success"] = "Member added.";
            }
            return RedirectToAction(nameof(Members), new { id = model.FacilityId });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteMember(int id, int facilityId)
        {
            var m = await _context.FacilityMembers.FindAsync(id);
            if (m != null) { m.IsDeleted = true; await _context.SaveChangesAsync(); }
            return RedirectToAction(nameof(Members), new { id = facilityId });
        }

        // ── HELPERS ───────────────────────────────────────
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
