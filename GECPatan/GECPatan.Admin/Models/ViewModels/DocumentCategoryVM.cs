using System.ComponentModel.DataAnnotations;

namespace GECPatan.Admin.Models.ViewModels
{
    public class DocumentCategoryVM
    {
        public int Id { get; set; }

        [Required(ErrorMessage = "Title is required")]
        [MaxLength(200)]
        public string Title { get; set; } = string.Empty;

        public int DisplayOrder { get; set; } = 0;
        public bool IsVisible { get; set; } = true;

        public int YearCount { get; set; }
        public int FileCount { get; set; }
    }

    public class DocumentYearSectionVM
    {
        public int Id { get; set; }
        public int CategoryId { get; set; }
        public string? CategoryTitle { get; set; }

        [Required(ErrorMessage = "Year is required")]
        [MaxLength(20)]
        public string Year { get; set; } = string.Empty;

        public int DisplayOrder { get; set; } = 0;
        public int FileCount { get; set; }
    }

    public class DocumentFileVM
    {
        public int Id { get; set; }
        public int YearSectionId { get; set; }
        public string? YearSectionLabel { get; set; }
        public string? CategoryTitle { get; set; }
        public int CategoryId { get; set; }

        [Required(ErrorMessage = "Title is required")]
        [MaxLength(300)]
        public string Title { get; set; } = string.Empty;

        public bool IsVisible { get; set; } = true;
        public int DisplayOrder { get; set; } = 0;
        public string? ExistingFilePath { get; set; }
    }
}