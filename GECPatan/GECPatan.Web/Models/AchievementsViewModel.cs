namespace GECPatan.Web.Models
{
    // Powers the tab bar on GET /Achievements (Views/Achievement/Index.cshtml).
    // The achievement cards themselves are loaded afterwards via AJAX into
    // #achDiv from GET /Achievements/ByYear?year=... (see AchievementListViewModel),
    // same pattern the old Academics/AchievementsByYear partial used.
    public class AchievementsViewModel
    {
        public List<int> Years { get; set; } = new();
    }
}
