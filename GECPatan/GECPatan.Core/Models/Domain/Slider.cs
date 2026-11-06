using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GECPatan.Core.Models.Domain
{
    // SLIDER
    public class Slider : BaseEntity
    {
        public int Id { get; set; }

        public string? ImagePath { get; set; }

        [MaxLength(300)]
        public string? H3Text { get; set; }

        [MaxLength(300)]
        public string? H4Text { get; set; }

        [MaxLength(300)]
        public string? H5Text { get; set; }

        [MaxLength(200)]
        public string? Anchor1Text { get; set; }

        [MaxLength(500)]
        public string? Anchor1Link { get; set; }

        [MaxLength(200)]
        public string? Anchor2Text { get; set; }

        [MaxLength(500)]
        public string? Anchor2Link { get; set; }

        public int DisplayOrder { get; set; } = 0;
        public bool IsVisible { get; set; } = true;
    }
}
