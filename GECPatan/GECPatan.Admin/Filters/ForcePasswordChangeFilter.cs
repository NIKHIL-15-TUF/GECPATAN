using System.Security.Claims;
using GECPatan.Core.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.EntityFrameworkCore;

namespace GECPatan.Admin.Filters
{
    /// <summary>
    /// While a user still has MustChangePassword = true, every page they open
    /// sends them to the Change Password page -- so clicking the sidebar can no
    /// longer silently bypass it. The page has a "Change later" button which sets
    /// the SkipCookie for the rest of the browser session; the next login asks again.
    /// </summary>
    public class ForcePasswordChangeFilter : IAsyncActionFilter
    {
        public const string SkipCookie = "pwd_skip";

        private readonly ApplicationDbContext _db;
        public ForcePasswordChangeFilter(ApplicationDbContext db) => _db = db;

        public async Task OnActionExecutionAsync(ActionExecutingContext ctx, ActionExecutionDelegate next)
        {
            var http = ctx.HttpContext;
            var userId = http.User.FindFirstValue(ClaimTypes.NameIdentifier);

            if (userId == null                                   // not signed in
                || !HttpMethods.IsGet(http.Request.Method)       // only page navigations
                || http.Request.Headers["X-Requested-With"] == "XMLHttpRequest"
                || !http.Request.Headers.Accept.ToString().Contains("text/html", StringComparison.OrdinalIgnoreCase)
                || string.Equals(ctx.RouteData.Values["controller"]?.ToString(), "Account",
                                 StringComparison.OrdinalIgnoreCase))
            {
                await next();
                return;
            }

            var mustChange = await _db.Users.AsNoTracking()
                .Where(u => u.Id == userId)
                .Select(u => u.MustChangePassword)
                .FirstOrDefaultAsync();

            var skipped = http.Request.Cookies[SkipCookie] == userId;

            if (mustChange && !skipped)
            {
                ctx.Result = new RedirectToActionResult("ChangePassword", "Account", null);
                return;
            }

            await next();
        }
    }
}
