using GECPatan.Admin.Data;
using GECPatan.Admin.Models.Domain;
using GECPatan.Admin.Models.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace GECPatan.Admin.Controllers
{
    [Authorize(Roles = "SuperAdmin,ContentEditor,HOD")]
    public class StudentClubController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly IWebHostEnvironment _env;

        public StudentClubController(ApplicationDbContext context, IWebHostEnvironment env)
        {
            _context = context;
            _env = env;
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
                    Id           = c.Id,
                    Title        = c.Title,
                    ImageCount   = c.Images.Count,
                    IsVisible    = c.IsVisible,
                    DisplayOrder = c.DisplayOrder,
                    MemberCount  = c.Members.Count
                })
                .ToListAsync();
            return View(clubs);
        }

        public IActionResult Create()
        {
            ViewData["Title"] = "Add Club";
            return View(new StudentClubCreateVM());
        }

        [HttpPost][ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(StudentClubCreateVM model)
        {
            ViewData["Title"] = "Add Club";
            if (!ModelState.IsValid) return View(model);

            int maxOrder = await _context.StudentClubs
                .Select(c => (int?)c.DisplayOrder).MaxAsync() ?? -1;

            _context.StudentClubs.Add(new StudentClub
            {
                Title        = model.Title,
                About        = model.About,
                BlogLink     = model.BlogLink,
                DisplayOrder = maxOrder + 1,
                IsVisible    = model.IsVisible
            });

            await _context.SaveChangesAsync();
            TempData["Success"] = "Club added.";
            return RedirectToAction(nameof(Index));
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
                Id             = c.Id,
                Title          = c.Title,
                About          = c.About,
                BlogLink       = c.BlogLink,
                DisplayOrder   = c.DisplayOrder,
                IsVisible      = c.IsVisible,
                ExistingImages = c.Images
                    .Where(i => !i.IsDeleted)
                    .OrderBy(i => i.DisplayOrder)
                    .Select(i => new ClubImageVM
                    {
                        Id           = i.Id,
                        ImagePath    = i.ImagePath,
                        Caption      = i.Caption,
                        DisplayOrder = i.DisplayOrder,
                        ClubId       = i.ClubId
                    }).ToList()
            });
        }

        [HttpPost][ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, StudentClubEditVM model)
        {
            ViewData["Title"] = "Edit Club";
            if (!ModelState.IsValid) return View(model);

            var c = await _context.StudentClubs.FindAsync(id);
            if (c == null) return NotFound();

            c.Title        = model.Title;
            c.About        = model.About;
            c.BlogLink     = model.BlogLink;
            c.DisplayOrder = model.DisplayOrder;
            c.IsVisible    = model.IsVisible;

            // Add new images
            int order = await _context.ClubImages
                .Where(i => i.ClubId == id)
                .Select(i => (int?)i.DisplayOrder).MaxAsync() ?? -1;

            foreach (var file in Request.Form.Files.Where(f => f.Name == "NewImages"))
            {
                if (file.Length > 0)
                {
                    _context.ClubImages.Add(new ClubImage
                    {
                        ClubId       = id,
                        ImagePath    = await SaveFileAsync(file, "clubs"),
                        Caption      = Path.GetFileNameWithoutExtension(file.FileName),
                        DisplayOrder = ++order
                    });
                }
            }

            await _context.SaveChangesAsync();
            TempData["Success"] = "Club updated.";
            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        public async Task<IActionResult> DeleteImage(int imageId, int clubId)
        {
            var img = await _context.ClubImages.FindAsync(imageId);
            if (img != null)
            {
                DeleteFile(img.ImagePath);
                img.IsDeleted = true;
                await _context.SaveChangesAsync();
                TempData["Success"] = "Image removed.";
            }
            return RedirectToAction(nameof(Edit), new { id = clubId });
        }

        [HttpPost] public async Task<IActionResult> ToggleVisible(int id)
        {
            var c = await _context.StudentClubs.FindAsync(id);
            if (c == null) return NotFound();
            c.IsVisible = !c.IsVisible;
            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        [HttpPost] public async Task<IActionResult> Delete(int id)
        {
            var c = await _context.StudentClubs.FindAsync(id);
            if (c == null) return NotFound();
            c.IsDeleted = true;
            await _context.SaveChangesAsync();
            TempData["Success"] = "Club deleted.";
            return RedirectToAction(nameof(Index));
        }

        // ── MEMBERS ───────────────────────────────────────
        public async Task<IActionResult> Members(int id)
        {
            ViewData["Title"] = "Club Members";
            var club = await _context.StudentClubs.FindAsync(id);
            if (club == null) return NotFound();

            ViewBag.ClubId    = id;
            ViewBag.ClubTitle = club.Title;

            var members = await _context.ClubMembers
                .Where(m => m.ClubId == id && !m.IsDeleted)
                .OrderBy(m => m.DisplayOrder)
                .ToListAsync();
            return View(members);
        }

        [HttpPost][ValidateAntiForgeryToken]
        public async Task<IActionResult> AddMember(ClubMemberVM model)
        {
            if (ModelState.IsValid)
            {
                _context.ClubMembers.Add(new ClubMember
                {
                    ClubId       = model.ClubId,
                    Name         = model.Name,
                    Position     = model.Position,
                    Department   = model.Department,
                    Email        = model.Email,
                    DisplayOrder = model.DisplayOrder
                });
                await _context.SaveChangesAsync();
                TempData["Success"] = "Member added.";
            }
            return RedirectToAction(nameof(Members), new { id = model.ClubId });
        }

        [HttpPost][ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteMember(int id, int clubId)
        {
            var m = await _context.ClubMembers.FindAsync(id);
            if (m != null) { m.IsDeleted = true; await _context.SaveChangesAsync(); }
            return RedirectToAction(nameof(Members), new { id = clubId });
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
