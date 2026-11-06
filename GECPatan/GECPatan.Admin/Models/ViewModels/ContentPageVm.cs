using System.ComponentModel.DataAnnotations;

namespace GECPatan.Admin.Models.ViewModels
{
    /// <summary>
    /// One view model for both Create and Edit.
    /// Replaces ContentPageCreateVM and ContentPageEditVM.
    /// Id == 0 means a new page.
    /// </summary>
    public class ContentPageFormVM
    {
        public int Id { get; set; }

        [Required(ErrorMessage = "Title is required")]
        [MaxLength(300)]
        public string Title { get; set; } = string.Empty;

        [Display(Name = "Title Image")]
        public IFormFile? TitleImage { get; set; }

        // Auto-generated from title, can be overridden
        [MaxLength(300)]
        [Display(Name = "URL Slug")]
        public string? Slug { get; set; }

        public string? HtmlContent { get; set; }

        public bool IsVisible { get; set; } = true;

        [Display(Name = "Carousel Images")]
        public List<IFormFile>? CarouselImages { get; set; }

        // ── Edit-screen display data (not posted by the user) ──
        public string GeneratedUrl { get; set; } = string.Empty;
        public string? TitleImageUrl { get; set; }

        // Already-saved images, shown for preview/delete
        public List<ContentPageImageVM> ExistingImages { get; set; } = new();
    }
    public class ContentPageImageVM
    {
        public int Id { get; set; }
        public string ImageUrl { get; set; } = string.Empty;
        public string? Caption { get; set; }
        public int DisplayOrder { get; set; }
    }
    public class ContentPageListVM
    {
        public int Id { get; set; }
        public string Title { get; set; } = string.Empty;
        public string Slug { get; set; } = string.Empty;
        public string? CreatedBy { get; set; }
        public DateTime CreatedDate { get; set; }
        public bool IsVisible { get; set; }
        public string PublicUrl => $"/page/{Slug}";
    }
   
    public class ContentPageCreateVM
    {
        [Required(ErrorMessage = "Title is required")]
        [MaxLength(300)]
        public string Title { get; set; } = string.Empty;

        [Display(Name = "Title Image")]
        public IFormFile? TitleImage { get; set; }

        // Auto-generated from title, can be overridden
        [MaxLength(300)]
        [Display(Name = "URL Slug")]
        public string? Slug { get; set; }

        public string? HtmlContent { get; set; }

        public bool IsVisible { get; set; } = true;
        [Display(Name = "Carousel Images")]
        public List<IFormFile>? CarouselImages { get; set; }
    }

    public class ContentPageEditVM : ContentPageCreateVM
    {
        public int Id { get; set; }
        public string GeneratedUrl { get; set; } = string.Empty;
        public string? TitleImageUrl { get; set; }
        // Already-saved images, shown for reorder/preview/delete
        public List<ContentPageImageVM> ExistingImages { get; set; } = new();

    }
}