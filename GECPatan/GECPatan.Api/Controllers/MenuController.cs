using GECPatan.Api.Common;
using GECPatan.Api.DTOs;
using GECPatan.Core.Data;
using GECPatan.Core.Models.Domain;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace GECPatan.Api.Controllers
{
    [ApiController]
    [Route("api/menu")]
    public class MenuController : ControllerBase
    {
        private readonly ApplicationDbContext _context;

        public MenuController(ApplicationDbContext context)
            => _context = context;
        // GET /api/menu/top
        // Top-bar quick action links (e.g. Alumni, RTI, Recruitment)
        [HttpGet("top")]
        public async Task<ActionResult<ApiResponse<List<MenuItemDTO>>>> GetTop()
            => await BuildMenuTree("Top");

        // GET /api/menu/main
        // 3-level hierarchical navbar menu tree
        [HttpGet("main")]
        public async Task<ActionResult<ApiResponse<List<MenuItemDTO>>>> GetMain()
            => await BuildMenuTree("Main");

        // GET /api/menu/footer
        // Footer menu tree
        [HttpGet("footer")]
        public async Task<ActionResult<ApiResponse<List<MenuItemDTO>>>> GetFooter()
            => await BuildMenuTree("Footer");

        // ── HELPERS ───────────────────────────────────────
        private async Task<ActionResult<ApiResponse<List<MenuItemDTO>>>> BuildMenuTree(
            string menuType)
        {
            var allItems = await _context.MenuItems
                .Where(m => m.MenuType == menuType && m.IsVisible)
                .OrderBy(m => m.Position)
                .ToListAsync();

            // Preload content page slugs (needed to resolve
            // DynamicType=ContentPage links)
            var pageIds = allItems.Where(m => m.DynamicType == "ContentPage")
                .Select(m => m.DynamicId).Where(id => id.HasValue)
                .Select(id => id!.Value).ToList();

            var pageSlugs = await _context.ContentPages
                .Where(p => pageIds.Contains(p.Id))
                .ToDictionaryAsync(p => p.Id, p => p.Slug);

            // Build flat DTO list first
            var dtoLookup = allItems.ToDictionary(m => m.Id, m => new MenuItemDTO
            {
                Id = m.Id,
                MenuText = m.MenuText,
                LinkType = m.LinkType,
                CssClass = m.CssClass,
                Position = m.Position,
                OpenInNewTab = m.OpenInNewTab,
                Link = ResolveLink(m, pageSlugs)
            });

            // Build tree
            var roots = new List<MenuItemDTO>();

            foreach (var item in allItems)
            {
                var dto = dtoLookup[item.Id];

                if (item.ParentId.HasValue && dtoLookup.ContainsKey(item.ParentId.Value))
                    dtoLookup[item.ParentId.Value].Children.Add(dto);
                else
                    roots.Add(dto);
            }

            // Sort children by position recursively
            SortChildren(roots);

            return Ok(ApiResponse<List<MenuItemDTO>>.Ok(roots));
        }

        private static void SortChildren(List<MenuItemDTO> items)
        {
            items.Sort((a, b) => a.Position.CompareTo(b.Position));
            foreach (var item in items)
                SortChildren(item.Children);
        }

        private static string? ResolveLink(
            MenuItem m,
            Dictionary<int, string> pageSlugs)
        {
            switch (m.LinkType)
            {
                case "internal":
                    if (!string.IsNullOrEmpty(m.ControllerName)
                        && !string.IsNullOrEmpty(m.ActionName))
                        return $"/{m.ControllerName}/{m.ActionName}";
                    return null;

                case "dynamic":
                    if (!m.DynamicId.HasValue) return null;

                    return m.DynamicType switch
                    {
                        "Department" => $"/Department/{m.DynamicId}",
                        "Committee" => $"/Committee/{m.DynamicId}",
                        "Facility" => $"/Facilities/{m.DynamicId}",
                        "Club" => $"/Clubs/{m.DynamicId}",
                        "ContentPage" => pageSlugs.ContainsKey(m.DynamicId.Value)
                            ? $"/page/{pageSlugs[m.DynamicId.Value]}"
                            : null,
                        "Document" => $"/documents/{m.DynamicId}",
                        _ => !string.IsNullOrEmpty(m.ControllerName)
                                     && !string.IsNullOrEmpty(m.ActionName)
                            ? $"/{m.ControllerName}/{m.ActionName}/{m.DynamicId}"
                            : null
                    };

                case "external":
                    return m.ExternalLink;

                default: // "none" — container, no link
                    return null;
            }
        }
    }
}