using GECPatan.Core.Data;
using  GECPatan.Core.Models.Domain;
using GECPatan.Admin.Models.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace GECPatan.Admin.Controllers
{
    [Authorize(Roles = "SuperAdmin,ContentEditor,HOD,Principal")]
    public class ResearchGrantController : Controller
    {
        private readonly ApplicationDbContext _context;

        public ResearchGrantController(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<IActionResult> Index()
        {
            ViewData["Title"] = "Research Grants";
            var items = await _context.ResearchGrants
                .OrderByDescending(r => r.Id)
                .ToListAsync();
            return View(items);
        }

        public IActionResult Create()
        {
            ViewData["Title"] = "Add Research Grant";
            return View(new ResearchGrantVM());
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(ResearchGrantVM model)
        {
            ViewData["Title"] = "Add Research Grant";
            if (!ModelState.IsValid) return View(model);

            _ = _context.ResearchGrants.Add(new ResearchGrant
            {
                Title = model.Title,
                PrincipalInvestigator = model.PrincipalInvestigator,
                StartDate = model.StartDate,
                CompletionDate = model.CompletionDate,
                Duration = model.Duration,
                ProjectCost = model.ProjectCost,
                SponsoringAuthority = model.SponsoringAuthority,
                IsVisible = model.IsVisible,
                DisplayOrder = model.DisplayOrder
            });

            await _context.SaveChangesAsync();
            TempData["Success"] = "Research Grant added.";
            return RedirectToAction(nameof(Index));
        }

        public async Task<IActionResult> Edit(int id)
        {
            ViewData["Title"] = "Edit Research Grant";
            var r = await _context.ResearchGrants.FindAsync(id);
            if (r == null) return NotFound();

            return View(new ResearchGrantVM
            {
                Id = r.Id,
                Title = r.Title,
                PrincipalInvestigator = r.PrincipalInvestigator,
                StartDate = r.StartDate,
                CompletionDate = r.CompletionDate,
                Duration = r.Duration,
                ProjectCost = r.ProjectCost,
                SponsoringAuthority = r.SponsoringAuthority,
                IsVisible = r.IsVisible,
                DisplayOrder = r.DisplayOrder
            });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, ResearchGrantVM model)
        {
            ViewData["Title"] = "Edit Research Grant";
            if (!ModelState.IsValid) return View(model);

            var r = await _context.ResearchGrants.FindAsync(id);
            if (r == null) return NotFound();

            r.Title = model.Title;
            r.PrincipalInvestigator = model.PrincipalInvestigator;
            r.StartDate = model.StartDate;
            r.CompletionDate = model.CompletionDate;
            r.Duration = model.Duration;
            r.ProjectCost = model.ProjectCost;
            r.SponsoringAuthority = model.SponsoringAuthority;
            r.IsVisible = model.IsVisible;
            r.DisplayOrder = model.DisplayOrder;

            await _context.SaveChangesAsync();
            TempData["Success"] = "Research Grant updated.";
            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        public async Task<IActionResult> ToggleVisible(int id)
        {
            var r = await _context.ResearchGrants.FindAsync(id);
            if (r == null) return NotFound();
            r.IsVisible = !r.IsVisible;
            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        public async Task<IActionResult> Delete(int id)
        {
            var r = await _context.ResearchGrants.FindAsync(id);
            if (r == null) return NotFound();
            r.IsDeleted = true;
            await _context.SaveChangesAsync();
            TempData["Success"] = "Deleted.";
            return RedirectToAction(nameof(Index));
        }
    }
}