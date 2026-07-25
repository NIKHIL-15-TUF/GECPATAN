using GECPatan.Admin.Models.ViewModels;
using GECPatan.Core.Data;
using GECPatan.Core.Models.Domain;
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

            int maxOrder = await _context.StudentClubs
                .Select(c => (int?)c.DisplayOrder).MaxAsync() ?? -1;

            // Cover image is uploaded separately from the carousel images,
            // and is stored directly on the StudentClub row (CoverImagePath).
            string? coverImagePath = null;
            if (model.CoverImageFile is { Length: > 0 })
            {
                coverImagePath = await SaveFileAsync(model.CoverImageFile, "clubs");
            }

            _context.StudentClubs.Add(new StudentClub
            {
                Title = model.Title,
                About = model.About,
                BlogLink = model.BlogLink,
                CoverImagePath = coverImagePath,
                DisplayOrder = maxOrder + 1,
                IsVisible = model.IsVisible
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

            c.Title = model.Title;
            c.About = model.About;
            c.BlogLink = model.BlogLink;
            c.DisplayOrder = model.DisplayOrder;
            c.IsVisible = model.IsVisible;

            // Cover image: only touched if a new file was uploaded. Leaving
            // the field empty on the form keeps whatever is already saved.
            if (model.CoverImageFile is { Length: > 0 })
            {
                DeleteFile(c.CoverImagePath);
                c.CoverImagePath = await SaveFileAsync(model.CoverImageFile, "clubs");
            }

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
                        ClubId = id,
                        ImagePath = await SaveFileAsync(file, "clubs"),
                        Caption = Path.GetFileNameWithoutExtension(file.FileName),
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

        [HttpPost]
        public async Task<IActionResult> ToggleVisible(int id)
        {
            var c = await _context.StudentClubs.FindAsync(id);
            if (c == null) return NotFound();
            c.IsVisible = !c.IsVisible;
            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        public async Task<IActionResult> Delete(int id)
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

                if (Photo is { Length: > 0 })
                {
                    member.ImagePath = await SaveFileAsync(Photo, "clubs/members");
                }
            }

            _context.ClubMembers.Add(member);
            await _context.SaveChangesAsync();
            TempData["Success"] = "Member added.";
            return RedirectToAction(nameof(Members), new { id = model.ClubId });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteMember(int id, int clubId)
        {
            var m = await _context.ClubMembers.FindAsync(id);
            if (m != null) { m.IsDeleted = true; await _context.SaveChangesAsync(); TempData["Success"] = "Member removed."; }
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