using GECPatan.Web.Models.Dtos;

namespace GECPatan.Web.Models
{
    // Populates Views/Achievement/_AchievementList.cshtml, which is AJAX-loaded
    // into #achDiv by loadAchievements() in Views/Achievement/Index.cshtml
    // (mirrors how the old _Achievements.cshtml partial worked, but the
    // CubePortfolio filter buttons are now built from TypeName instead of
    // the old free-text Keywords field).
    public class AchievementListViewModel
    {
        // Uploaded images (achievement photos) are served by GECPatan.Api's
        // own static file middleware, not by this Web app -- see
        // ResolveApiFileUrl() in the partial, same reasoning as
        // DepartmentViewModel.ApiBaseUrl.
        public string ApiBaseUrl { get; set; } = string.Empty;

        public List<AchievementDTO> Achievements { get; set; } = new();

        // Distinct TypeName values present in Achievements, used to build the
        // CubePortfolio filter buttons.
        public List<string> Types { get; set; } = new();
    }
}
