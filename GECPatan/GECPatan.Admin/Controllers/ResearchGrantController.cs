using GECPatan.Core.Data;
using GECPatan.Core.Models.Domain;
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
        private readonly ILogger<ResearchGrantController> _logger;

        public ResearchGrantController(ApplicationDbContext context, ILogger<ResearchGrantController> logger)
        {
            _context = context;
            _logger = logger;
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

            var grant = new ResearchGrant
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
            };

            try
            {
                _context.ResearchGrants.Add(grant);
                await _context.SaveChangesAsync();

                _logger.LogInformation("Research grant {GrantId} '{Title}' created", grant.Id, grant.Title);

                TempData["Success"] = "Research Grant added.";
                return RedirectToAction(nameof(Index));
            }
            catch (DbUpdateException ex)
            {
                _logger.LogError(ex, "Database error creating research grant '{Title}'", model.Title);
                ModelState.AddModelError(string.Empty, "Unable to save the research grant. Please try again.");
                return View(model);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error creating research grant '{Title}'", model.Title);
                ModelState.AddModelError(string.Empty, "An unexpected error occurred. Please try again.");
                return View(model);
            }
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

            try
            {
                await _context.SaveChangesAsync();

                _logger.LogInformation("Research grant {GrantId} '{Title}' updated", r.Id, r.Title);

                TempData["Success"] = "Research Grant updated.";
                return RedirectToAction(nameof(Index));
            }
            catch (DbUpdateConcurrencyException ex)
            {
                _logger.LogWarning(ex, "Concurrency conflict updating research grant {GrantId}", id);
                ModelState.AddModelError(string.Empty,
                    "This record was changed by someone else. Please reload and try again.");
                return View(model);
            }
            catch (DbUpdateException ex)
            {
                _logger.LogError(ex, "Database error updating research grant {GrantId}", id);
                ModelState.AddModelError(string.Empty, "Unable to save the research grant. Please try again.");
                return View(model);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error updating research grant {GrantId}", id);
                ModelState.AddModelError(string.Empty, "An unexpected error occurred. Please try again.");
                return View(model);
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ToggleVisible(int id)
        {
            var r = await _context.ResearchGrants.FindAsync(id);
            if (r == null) return NotFound();

            r.IsVisible = !r.IsVisible;

            try
            {
                await _context.SaveChangesAsync();
                _logger.LogInformation("Research grant {GrantId} visibility set to {IsVisible}", id, r.IsVisible);
            }
            catch (DbUpdateException ex)
            {
                _logger.LogError(ex, "Database error toggling visibility for research grant {GrantId}", id);
                TempData["Error"] = "Unable to update visibility. Please try again.";
            }

            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int id)
        {
            var r = await _context.ResearchGrants.FindAsync(id);
            if (r == null) return NotFound();

            r.IsDeleted = true;

            try
            {
                await _context.SaveChangesAsync();

                _logger.LogInformation("Research grant {GrantId} '{Title}' deleted", id, r.Title);
                TempData["Success"] = "Deleted.";
            }
            catch (DbUpdateException ex)
            {
                _logger.LogError(ex, "Database error deleting research grant {GrantId}", id);
                TempData["Error"] = "Unable to delete the record. Please try again.";
            }

            return RedirectToAction(nameof(Index));
        }
    }
}