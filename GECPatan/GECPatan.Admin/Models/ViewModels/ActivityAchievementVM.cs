using Microsoft.AspNetCore.Mvc.Rendering;
using System.ComponentModel.DataAnnotations;

namespace GECPatan.Admin.Models.ViewModels
{
    // ── ACTIVITY ───
    public class ActivityListVM
    {
        public int Id { get; set; }
        public string Title { get; set; } = string.Empty;
        public string? EventDate { get; set; }
        public string? DeptName { get; set; }
        public string? CommitteeName { get; set; }
        public bool IsVisible { get; set; }
        public int ImageCount { get; set; }
        public int FileCount { get; set; }
    }

    public class ActivityCreateVM
    {
        [Required(ErrorMessage = "Title is required")]
        [MaxLength(300)]
        public string Title { get; set; } = string.Empty;

        public string? Description { get; set; }

        [Display(Name = "Event Date")]
        [DataType(DataType.Date)]
        public DateTime? EventDate { get; set; }

        [Display(Name = "Event Time")]
        public string? EventTime { get; set; }

        public int? Year { get; set; }

        [Display(Name = "Target Students")]
        public string? TargetStudents { get; set; }

        public string? Keywords { get; set; }
        public string? ExternalLink { get; set; }

        [Display(Name = "Department")]
        public int? DeptId { get; set; }

        [Display(Name = "Committee")]
        public int? CommitteeId { get; set; }

        [Display(Name = "Club")]
        public int? ClubId { get; set; }

        public bool IsVisible { get; set; } = true;
        public bool IsFile { get; set; } = true;

        // Dropdowns
        public List<SelectListItem> Departments { get; set; } = new();
        public List<SelectListItem> Committees { get; set; } = new();
        public List<SelectListItem> Clubs { get; set; } = new();
    }

    public class ActivityEditVM : ActivityCreateVM
    {
        public int Id { get; set; }
    }

    // ── ACHIEVEMENT ──
    public class AchievementListVM
    {
        public int Id { get; set; }
        public string Title { get; set; } = string.Empty;

        // Was: string? Date. Now DateTime? -- format on display with
        // Date?.ToString("dd MMM yyyy") in the view (see Index.cshtml).
        public DateTime? Date { get; set; }
        public string? DeptName { get; set; }
        public int? Year { get; set; }
        public bool IsVisible { get; set; }
        public string? ImagePath { get; set; }
    }

    public class AchievementCreateVM
    {
        [Required(ErrorMessage = "Title is required")]
        [MaxLength(400)]
        public string Title { get; set; } = string.Empty;

        public string? Description { get; set; }

        // Was: string? Date with a "e.g. 2025/12/13" placeholder and a plain
        // text input. Now a real date, bound to <input type="date"> --
        // DataType.Date + the DisplayFormat make asp-for render/parse it
        // correctly (see Create.cshtml / Edit.cshtml).
        [Display(Name = "Date")]
        [DataType(DataType.Date)]
        [DisplayFormat(DataFormatString = "{0:yyyy-MM-dd}", ApplyFormatInEditMode = true)]
        public DateTime? Date { get; set; }

        public int? Year { get; set; }
        public string? Keywords { get; set; }

        [Display(Name = "Type")]
        public int Type { get; set; } = 1;

        [Display(Name = "Department")]
        public int? DeptId { get; set; }

        [Display(Name = "Department Name (display)")]
        public string? DeptName { get; set; }

        [Display(Name = "Committee")]
        public int? CommitteeId { get; set; }

        public bool IsVisible { get; set; } = true;
        public string? ExistingImagePath { get; set; }

        public List<SelectListItem> Departments { get; set; } = new();
        public List<SelectListItem> Committees { get; set; } = new();
        public List<SelectListItem> Types { get; set; } = new();
    }

    public class AchievementEditVM : AchievementCreateVM
    {
        public int Id { get; set; }
    }
}
