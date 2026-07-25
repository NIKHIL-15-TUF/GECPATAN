namespace GECPatan.Web.Models.Dtos
{
    // Mirrors GECPatan.Api.DTOs.StudentClubListDTO (from GET /api/clubs).
    public class StudentClubListDTO
    {
        public int Id { get; set; }
        public string Title { get; set; } = string.Empty;
        public string? CoverImage { get; set; } // dedicated cover image, set separately from Images
        public int DisplayOrder { get; set; }
        public string? Link { get; set; } // resolved link if dynamic
    }

    // Mirrors GECPatan.Api.DTOs.StudentClubDetailDTO (from GET /api/clubs/{id}).
    public class StudentClubDetailDTO
    {
        public int Id { get; set; }
        public string Title { get; set; } = string.Empty;
        public string? About { get; set; }
        public string? BlogLink { get; set; }
        public string? CoverImage { get; set; } // dedicated cover image, set separately from Images
        public string? Link { get; set; } // resolved internal link if dynamic

        public List<ClubImageDTO> Images { get; set; } = new();
        public List<ClubMemberDTO> Members { get; set; } = new();
        public List<DynamicSectionDTO> DynamicSections { get; set; } = new();
        public List<string> Objectives { get; set; } = new();
    }

    public class ClubImageDTO
    {
        public string ImagePath { get; set; } = string.Empty;
        public string? Caption { get; set; }
        public int DisplayOrder { get; set; }
    }

    public class ClubMemberDTO
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string? Position { get; set; }
        public string? Department { get; set; }
        public string? ImagePath { get; set; }
        public int DisplayOrder { get; set; }
        // Email intentionally excluded from public API
    }

    // NOTE: ActivityDTO / ActivityImageDTO / ActivityFileDTO already exist in
    // GECPatan.Web/Models/DTOs/DepartmentApiModels.cs and are reused here for
    // GET /api/clubs/{id}/activities, so they are not redeclared in this file.
}