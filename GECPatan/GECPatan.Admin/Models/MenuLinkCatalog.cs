namespace GECPatan.Admin.Models
{
    public record TabOption(string Value, string Label);

    public record InternalPageInfo(string Key, string Label, string Group);

    /// <summary>
    /// Single place that tells the Menu form which public pages and tabs exist.
    /// A tab's Value MUST equal the id of the matching &lt;div class="tabcontent"&gt;
    /// on the Web page (that id is what goes after the # in the URL).
    /// When you add a page or tab to the Web project, add it here too.
    /// </summary>
    public static class MenuLinkCatalog
    {
        // ── Static pages (Link type: Internal) ─────────────────────────
        // Key = "Controller/Action" (this is what is stored in the database).
        public static readonly InternalPageInfo[] InternalPages =
        {
            new("Home/Index",                  "Home",                 "General"),
            new("ContactUs/Index",             "Contact Us",           "General"),
            new("Gallery/Index",               "Gallery",              "General"),
            new("Institute/NewsLetter",        "News Letter",          "General"),
            new("ResearchGrants/Index",        "Research Grants",      "General"),
            new("Achievement/Index",           "Achievements",         "General"),   // tabs = years (from DB)
            new("StudentCorner/StudentClubs",  "Student Clubs (list)", "Student Corner"),
            new("Documents/MoU",               "MoU",                  "Documents"),
            new("Documents/SSIP",              "SSIP",                 "Documents"),
            new("Documents/Timetable",         "Time Table",           "Documents"),
        };

        // ── Fixed tabs of the dynamic page types ───────────────────────
        // Custom sections added by the admin are appended automatically.
        public static readonly Dictionary<string, TabOption[]> DynamicTabs = new()
        {
            ["Department"] = new TabOption[]
            {
                new("About",         "About"),
                new("VisionMission", "Vision & Mission"),
                new("PeoPso",        "PEOs & PSOs"),
                new("Labs",          "Labs"),
                new("Staff",         "Faculties And Staff"),
                new("TimeTable",     "Time Table"),
                new("NoticeBoard",   "Notice Board"),
                new("Activities",    "Activities"),
                new("Achievements",  "Achievements"),
            },
            ["Committee"] = new TabOption[]
            {
                new("About",         "About"),
                new("VisionMission", "Vision & Mission"),
                new("Objectives",    "Objectives"),
                new("Members",       "Members"),
                new("Activities",    "Activities"),
            },
            ["Facility"] = new TabOption[]
            {
                new("About",         "About"),
                new("VisionMission", "Vision & Mission"),
                new("Members",       "Members"),
            },
            ["Club"] = new TabOption[]
            {
                new("About",         "About"),
                new("SubObjectives", "Sub Objectives"),
                new("Members",       "Members"),
                new("Activities",    "Activities"),
            },
            // "Document" and "ContentPage" have no tabs.
        };
    }
}
