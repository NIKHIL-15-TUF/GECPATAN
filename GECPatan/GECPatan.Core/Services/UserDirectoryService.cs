using GECPatan.Core.Data;
using GECPatan.Core.Models.Domain;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace GECPatan.Core.Services.UserManagement
{
    /// <summary>
    /// Read-only lookups needed to render the user list: each user's primary
    /// role, and the human-readable name of whatever they're assigned to
    /// (department, committee, facility, faculty profile, or content page).
    ///
    /// This exists specifically to replace the N+1 query pattern that used to
    /// live in UserManagementController.Index() — that version called
    /// UserManager.GetRolesAsync() plus one FindAsync() per user, inside a
    /// foreach loop. For N users that's up to 2N extra round-trips on a
    /// single page load. Everything here is 3 queries total, regardless of
    /// how many users are being displayed.
    /// </summary>
    public class UserDirectoryService
    {
        private readonly ApplicationDbContext _context;

        public UserDirectoryService(ApplicationDbContext context)
        {
            _context = context;
        }

        /// <summary>
        /// Returns, for every given user ID, their first assigned role name
        /// (or "—" if none). Single query regardless of user count.
        /// </summary>
        public async Task<Dictionary<string, string>> GetPrimaryRolesAsync(IEnumerable<string> userIds)
        {
            var idList = userIds.ToList();
            if (idList.Count == 0) return new Dictionary<string, string>();

            // IdentityDbContext doesn't expose the AspNetUserRoles join table as
            // a named DbSet, but it's still part of the model — Set<T>() reaches it.
            var userRoles = await _context.Set<IdentityUserRole<string>>()
                .Where(ur => idList.Contains(ur.UserId))
                .ToListAsync();

            var roleNamesById = await _context.Roles
                .ToDictionaryAsync(r => r.Id, r => r.Name ?? "—");

            // A user could technically hold more than one role; the original
            // behavior (FirstOrDefault) is preserved here for compatibility —
            // "primary" role is whichever the join table returns first.
            return userRoles
                .GroupBy(ur => ur.UserId)
                .ToDictionary(
                    g => g.Key,
                    g => roleNamesById.GetValueOrDefault(g.First().RoleId, "—"));
        }

        /// <summary>
        /// Returns, for every given user, the display name of whatever single
        /// thing they're assigned to (department/committee/facility/faculty
        /// profile/content page — a user has at most one of these set).
        /// Five queries total (one per assignment type), regardless of how
        /// many users are being displayed, instead of one query per user.
        /// </summary>
        public async Task<Dictionary<string, string>> GetAssignmentNamesAsync(IEnumerable<ApplicationUser> users)
        {
            var userList = users.ToList();
            var result = new Dictionary<string, string>();

            var deptIds = userList.Where(u => u.DeptId.HasValue).Select(u => u.DeptId!.Value).Distinct().ToList();
            var deptNames = deptIds.Count == 0
                ? new Dictionary<int, string>()
                : await _context.Departments
                    .Where(d => deptIds.Contains((int)d.DeptId))
                    .ToDictionaryAsync(d => (int)d.DeptId, d => d.Name);

            var commIds = userList.Where(u => u.CommitteeId.HasValue).Select(u => u.CommitteeId!.Value).Distinct().ToList();
            var commNames = commIds.Count == 0
                ? new Dictionary<int, string>()
                : await _context.CampusCommittees
                    .Where(c => commIds.Contains(c.Id))
                    .ToDictionaryAsync(c => c.Id, c => c.Title);

            var facilityIds = userList.Where(u => u.FacilityId.HasValue).Select(u => u.FacilityId!.Value).Distinct().ToList();
            var facilityNames = facilityIds.Count == 0
                ? new Dictionary<int, string>()
                : await _context.Facilities
                    .Where(f => facilityIds.Contains(f.Id))
                    .ToDictionaryAsync(f => f.Id, f => f.Title);

            var facultyIds = userList.Where(u => u.FacultyId.HasValue).Select(u => u.FacultyId!.Value).Distinct().ToList();
            var facultyNames = facultyIds.Count == 0
                ? new Dictionary<int, string>()
                : await _context.Faculties
                    .Where(f => facultyIds.Contains(f.FacultyId))
                    .ToDictionaryAsync(f => f.FacultyId, f => f.Name);

            var pageIds = userList.Where(u => u.ContentPageId.HasValue).Select(u => u.ContentPageId!.Value).Distinct().ToList();
            var pageTitles = pageIds.Count == 0
                ? new Dictionary<int, string>()
                : await _context.ContentPages
                    .Where(p => pageIds.Contains(p.Id))
                    .ToDictionaryAsync(p => p.Id, p => p.Title);

            foreach (var u in userList)
            {
                string assignment = "";
                if (u.DeptId.HasValue) assignment = deptNames.GetValueOrDefault(u.DeptId.Value, "");
                else if (u.CommitteeId.HasValue) assignment = commNames.GetValueOrDefault(u.CommitteeId.Value, "");
                else if (u.FacilityId.HasValue) assignment = facilityNames.GetValueOrDefault(u.FacilityId.Value, "");
                else if (u.FacultyId.HasValue) assignment = facultyNames.GetValueOrDefault(u.FacultyId.Value, "");
                else if (u.ContentPageId.HasValue && pageTitles.TryGetValue(u.ContentPageId.Value, out var title))
                    assignment = $"Page: {title}";

                result[u.Id] = assignment;
            }

            return result;
        }
    }
}
