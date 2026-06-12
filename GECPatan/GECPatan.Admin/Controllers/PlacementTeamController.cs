//using GECPatan.Core.Data;
//using  GECPatan.Core.Models.Domain;
//using GECPatan.Admin.Models.ViewModels;
//using Microsoft.AspNetCore.Authorization;
//using Microsoft.AspNetCore.Mvc;
//using Microsoft.EntityFrameworkCore;

//namespace GECPatan.Admin.Controllers
//{
//    [Authorize(Roles = "SuperAdmin,PlacementOfficer")]
//    public class PlacementTeamController : Controller
//    {
//        private readonly ApplicationDbContext _context;
//        private readonly IWebHostEnvironment _env;

//        public PlacementTeamController(ApplicationDbContext context, IWebHostEnvironment env)
//        {
//            _context = context;
//            _env = env;
//        }

//        public async Task<IActionResult> Index()
//        {
//            ViewData["Title"] = "Placement Team";
//            var items = await _context.PlacementTeamMembers
//                .OrderBy(t => t.DisplayOrder)
//                .ToListAsync();
//            return View(items);
//        }

//        public IActionResult Create()
//        {
//            ViewData["Title"] = "Add Team Member";
//            return View(new PlacementTeamMemberVM());
//        }

//        [HttpPost]
//        [ValidateAntiForgeryToken]
//        public async Task<IActionResult> Create(PlacementTeamMemberVM model, IFormFile? Photo)
//        {
//            ViewData["Title"] = "Add Team Member";
//            if (!ModelState.IsValid) return View(model);

//            int maxOrder = await _context.PlacementTeamMembers
//                .Select(t => (int?)t.DisplayOrder).MaxAsync() ?? -1;

//            var member = new PlacementTeamMember
//            {
//                Name = model.Name,
//                Designation = model.Designation,
//                Email = model.Email,
//                Mobile = model.Mobile,
//                DisplayOrder = maxOrder + 1,
//                IsVisible = model.IsVisible
//            };

//            if (Photo != null && Photo.Length > 0)
//                member.ImagePath = await SaveFileAsync(Photo, "placement");

//            _context.PlacementTeamMembers.Add(member);
//            await _context.SaveChangesAsync();
//            TempData["Success"] = "Team member added.";
//            return RedirectToAction(nameof(Index));
//        }

//        public async Task<IActionResult> Edit(int id)
//        {
//            ViewData["Title"] = "Edit Team Member";
//            var t = await _context.PlacementTeamMembers.FindAsync(id);
//            if (t == null) return NotFound();

//            return View(new PlacementTeamMemberVM
//            {
//                Id = t.Id,
//                Name = t.Name,
//                Designation = t.Designation,
//                Email = t.Email,
//                Mobile = t.Mobile,
//                DisplayOrder = t.DisplayOrder,
//                IsVisible = t.IsVisible,
//                ExistingImagePath = t.ImagePath
//            });
//        }

//        [HttpPost]
//        [ValidateAntiForgeryToken]
//        public async Task<IActionResult> Edit(int id, PlacementTeamMemberVM model, IFormFile? Photo)
//        {
//            ViewData["Title"] = "Edit Team Member";
//            if (!ModelState.IsValid) return View(model);

//            var t = await _context.PlacementTeamMembers.FindAsync(id);
//            if (t == null) return NotFound();

//            t.Name = model.Name;
//            t.Designation = model.Designation;
//            t.Email = model.Email;
//            t.Mobile = model.Mobile;
//            t.IsVisible = model.IsVisible;

//            if (Photo != null && Photo.Length > 0)
//            {
//                DeleteFile(t.ImagePath);
//                t.ImagePath = await SaveFileAsync(Photo, "placement");
//            }

//            await _context.SaveChangesAsync();
//            TempData["Success"] = "Team member updated.";
//            return RedirectToAction(nameof(Index));
//        }

//        [HttpPost]
//        public async Task<IActionResult> ToggleVisible(int id)
//        {
//            var t = await _context.PlacementTeamMembers.FindAsync(id);
//            if (t == null) return NotFound();
//            t.IsVisible = !t.IsVisible;
//            await _context.SaveChangesAsync();
//            return RedirectToAction(nameof(Index));
//        }

//        [HttpPost]
//        public async Task<IActionResult> Delete(int id)
//        {
//            var t = await _context.PlacementTeamMembers.FindAsync(id);
//            if (t == null) return NotFound();
//            DeleteFile(t.ImagePath);
//            t.IsDeleted = true;
//            await _context.SaveChangesAsync();
//            TempData["Success"] = "Member deleted.";
//            return RedirectToAction(nameof(Index));
//        }

//        [HttpPost]
//        public async Task<IActionResult> Reorder(int id, string direction)
//        {
//            var t = await _context.PlacementTeamMembers.FindAsync(id);
//            if (t == null) return NotFound();

//            if (direction == "up")
//            {
//                var above = await _context.PlacementTeamMembers
//                    .Where(x => x.DisplayOrder == t.DisplayOrder - 1).FirstOrDefaultAsync();
//                if (above != null) { above.DisplayOrder++; t.DisplayOrder--; }
//            }
//            else
//            {
//                var below = await _context.PlacementTeamMembers
//                    .Where(x => x.DisplayOrder == t.DisplayOrder + 1).FirstOrDefaultAsync();
//                if (below != null) { below.DisplayOrder--; t.DisplayOrder++; }
//            }

//            await _context.SaveChangesAsync();
//            return RedirectToAction(nameof(Index));
//        }

//        private async Task<string> SaveFileAsync(IFormFile file, string folder)
//        {
//            var uploadsFolder = Path.Combine(_env.WebRootPath, "uploads", folder);
//            Directory.CreateDirectory(uploadsFolder);
//            var fileName = Guid.NewGuid() + Path.GetExtension(file.FileName);
//            var filePath = Path.Combine(uploadsFolder, fileName);
//            using var stream = new FileStream(filePath, FileMode.Create);
//            await file.CopyToAsync(stream);
//            return $"/uploads/{folder}/{fileName}";
//        }

//        private void DeleteFile(string? filePath)
//        {
//            if (string.IsNullOrEmpty(filePath)) return;
//            var fullPath = Path.Combine(_env.WebRootPath, filePath.TrimStart('/'));
//            if (System.IO.File.Exists(fullPath)) System.IO.File.Delete(fullPath);
//        }
//    }
//}