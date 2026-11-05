
using Microsoft.AspNetCore.Http;
using System.ComponentModel.DataAnnotations;

namespace GECPatan.Admin.Models.ViewModels
{
    public class NotableAlumniVM
    {
        public int ID { get; set; }

        [Required]
        [Display(Name = "Order Number")]
        public int OrderNo { get; set; }

        [Display(Name = "Show on Website")]
        public bool IsShow { get; set; } = true;

        [Display(Name = "Is IFrame")]
        public bool IsIframe { get; set; }

        [Display(Name = "IFrame URL")]
        public string? IFrame { get; set; }

        [Required]
        [StringLength(500)]
        public string Title { get; set; } = string.Empty;

        [Display(Name = "Alumni Image")]
        public IFormFile? ImageFile { get; set; }

        public string? Image { get; set; }
    }
}
