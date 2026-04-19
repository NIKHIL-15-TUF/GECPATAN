//using GECPatan.Admin.Data;
//using GECPatan.Admin.Models.Domain;
//using GECPatan.Admin.Models.ViewModels;
//using Microsoft.AspNetCore.Authorization;
//using Microsoft.AspNetCore.Mvc;
//using Microsoft.EntityFrameworkCore;

//namespace GECPatan.Admin.Controllers
//{
//    [Authorize(Roles = "SuperAdmin,ContentEditor,HOD")]
//    public class StudentClubController : Controller
//    {
//        private readonly ApplicationDbContext _context;
//        private readonly IWebHostEnvironment _env;

//        public StudentClubController(ApplicationDbContext context, IWebHostEnvironment env)
//        {
//            _context = context;
//            _env = env;
//        }

//        // ── INDEX ─────────────────────────────────────────
//        public async Task<IActionResult> Index()
//        {
//            ViewData["Title"] = "Student Clubs";
//            var clubs = await _context.StudentClubs
//                .Include(c => c.Members)
//                .OrderBy(c => c.DisplayOrder)
//                .Select(c => new StudentClubListVM
//                {
//                    Id = c.Id,
//                    Title = c.Title,
//                    Icon = c.Icon,
//                    IsVisible = c.IsVisible,
//                    DisplayOrder = c.DisplayOrder,
//                    MemberCount = c.Members.Count
//                })
//                .ToListAsync();
//            return View(clubs);
//        }

//        // ── CREATE ────────────────────────────────────────
//        public IActionResult Create()
//        {
//            ViewData["Title"] = "Add Student Club";
//            return View(new StudentClubCreateVM());
//        }

//        [HttpPost]
//        [ValidateAntiForgeryToken]
//        public async Task<IActionResult> Create(StudentClubCreateVM model)
//        {
//            ViewData["Title"] = "Add Student Club";
//            if (!ModelState.IsValid) return View(model);

//            int maxOrder = await _context.StudentClubs
//                .Select(c => (int?)c.DisplayOrder).MaxAsync() ?? -1;

//            _context.StudentClubs.Add(new StudentClub
//            {
//                Title = model.Title,
//                About = model.About,
//                Icon = model.Icon,
//                BlogLink = model.BlogLink,
//                ActionName = model.ActionName,
//                ControllerName = model.ControllerName,
//                IsDynamic = model.IsDynamic,
//                DynamicId = model.DynamicId,
//                DisplayOrder = maxOrder + 1,
//                IsVisible = model.IsVisible
//            });

//            await _context.SaveChangesAsync();
//            TempData["Success"] = "Student Club added.";
//            return RedirectToAction(nameof(Index));
//        }

//        // ── EDIT ──────────────────────────────────────────
//        public async Task<IActionResult> Edit(int id)
//        {
//            ViewData["Title"] = "Edit Student Club";
//            var c = await _context.StudentClubs.FindAsync(id);
//            if (c == null) return NotFound();

//            return View(new StudentClubEditVM
//            {
//                Id = c.Id,
//                Title = c.Title,
//                About = c.About,
//                Icon = c.Icon,
//                BlogLink = c.BlogLink,
//                ActionName = c.ActionName,
//                ControllerName = c.ControllerName,
//                IsDynamic = c.IsDynamic,
//                DynamicId = c.DynamicId,
//                DisplayOrder = c.DisplayOrder,
//                IsVisible = c.IsVisible
//            });
//        }

//        [HttpPost]
//        [ValidateAntiForgeryToken]
//        public async Task<IActionResult> Edit(int id, StudentClubEditVM model)
//        {
//            ViewData["Title"] = "Edit Student Club";
//            if (!ModelState.IsValid) return View(model);

//            var c = await _context.StudentClubs.FindAsync(id);
//            if (c == null) return NotFound();

//            c.Title = model.Title;
//            c.About = model.About;
//            c.Icon = model.Icon;
//            c.BlogLink = model.BlogLink;
//            c.ActionName = model.ActionName;
//            c.ControllerName = model.ControllerName;
//            c.IsDynamic = model.IsDynamic;
//            c.DynamicId = model.DynamicId;
//            c.DisplayOrder = model.DisplayOrder;
//            c.IsVisible = model.IsVisible;

//            await _context.SaveChangesAsync();
//            TempData["Success"] = "Club updated.";
//            return RedirectToAction(nameof(Index));
//        }

//        // ── TOGGLE / DELETE ───────────────────────────────
//        [HttpPost]
//        public async Task<IActionResult> ToggleVisible(int id)
//        {
//            var c = await _context.StudentClubs.FindAsync(id);
//            if (c == null) return NotFound();
//            c.IsVisible = !c.IsVisible;
//            await _context.SaveChangesAsync();
//            return RedirectToAction(nameof(Index));
//        }

//        [HttpPost]
//        public async Task<IActionResult> Delete(int id)
//        {
//            var c = await _context.StudentClubs.FindAsync(id);
//            if (c == null) return NotFound();
//            c.IsDeleted = true;
//            await _context.SaveChangesAsync();
//            TempData["Success"] = "Club deleted.";
//            return RedirectToAction(nameof(Index));
//        }

//        // ── MEMBERS ───────────────────────────────────────
//        public async Task<IActionResult> Members(int id)
//        {
//            ViewData["Title"] = "Club Members";
//            var club = await _context.StudentClubs.FindAsync(id);
//            if (club == null) return NotFound();

//            ViewBag.ClubId = id;
//            ViewBag.ClubTitle = club.Title;

//            var members = await _context.ClubMembers
//                .Where(m => m.ClubId == id)
//                .OrderBy(m => m.DisplayOrder)
//                .ToListAsync();

//            return View(members);
//        }

//        [HttpPost]
//        [ValidateAntiForgeryToken]
//        public async Task<IActionResult> AddMember(ClubMemberVM model, IFormFile? MemberPhoto)
//        {
//            if (ModelState.IsValid)
//            {
//                var member = new ClubMember
//                {
//                    ClubId = model.ClubId,
//                    Name = model.Name,
//                    Position = model.Position,
//                    Department = model.Department,
//                    Email = model.Email,
//                    DisplayOrder = model.DisplayOrder
//                };

//                if (MemberPhoto != null && MemberPhoto.Length > 0)
//                    member.ImagePath = await SaveFileAsync(MemberPhoto, "clubs");

//                _context.ClubMembers.Add(member);
//                await _context.SaveChangesAsync();
//                TempData["Success"] = "Member added.";
//            }
//            return RedirectToAction(nameof(Members), new { id = model.ClubId });
//        }

//        [HttpPost]
//        [ValidateAntiForgeryToken]
//        public async Task<IActionResult> DeleteMember(int id, int clubId)
//        {
//            var m = await _context.ClubMembers.FindAsync(id);
//            if (m != null) { m.IsDeleted = true; await _context.SaveChangesAsync(); }
//            return RedirectToAction(nameof(Members), new { id = clubId });
//        }

//        // ── HELPERS ───────────────────────────────────────
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
//    }
//}