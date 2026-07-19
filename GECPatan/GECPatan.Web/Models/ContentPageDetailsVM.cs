namespace GECPatan.Web.Models.ViewModels
{
    public class ContentPageImageVM
    {
        public string ImageUrl { get; set; } = string.Empty;
        public string? Caption { get; set; }
        public int DisplayOrder { get; set; }
    }

    public class ContentPageDynamicSectionFileVM
    {
        public string FilePath { get; set; } = string.Empty;
        public string? Title { get; set; }
        public string? FileType { get; set; }
        public int DisplayOrder { get; set; }
    }

    public class ContentPageDynamicSectionVM
    {
        public int Id { get; set; }
        public string Title { get; set; } = string.Empty;
        public string SectionType { get; set; } = string.Empty;
        public string? HtmlContent { get; set; }
        public string? FilePath { get; set; }
        public string? FileName { get; set; }
        public int DisplayOrder { get; set; }
        public List<ContentPageDynamicSectionFileVM> Files { get; set; } = new();
    }

    public class ContentPageDetailsVM
    {
        public int Id { get; set; }
        public string Title { get; set; } = string.Empty;
        public string Slug { get; set; } = string.Empty;
        public string? HtmlContent { get; set; }

        // Fully-resolved (API host prefixed) image URLs, in DisplayOrder
        public List<ContentPageImageVM> Images { get; set; } = new();

        public List<ContentPageDynamicSectionVM> DynamicSections { get; set; } = new();

        public bool HasImages => Images.Any();
    }
}
