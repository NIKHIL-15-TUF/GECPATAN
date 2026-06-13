namespace GECPatan.Api.DTOs
{
    // ── FACILITY LIST (for /api/facilities) ────────────────
    public class FacilityListDTO
    {
        public int Id { get; set; }
        public string Title { get; set; } = string.Empty;
        public string? Tagline { get; set; }
        public string? TitleImagePath { get; set; }
        public int DisplayOrder { get; set; }
        public int MemberCount { get; set; }
    }

    // ── FACILITY DETAIL (for /api/facilities/{id}) ─────────
    public class FacilityDetailDTO
    {
        public int Id { get; set; }
        public string Title { get; set; } = string.Empty;
        public string? Tagline { get; set; }
        public string? About { get; set; }
        public string? TitleImagePath { get; set; }

        public List<string> BannerImages { get; set; } = new();
        public List<string> Visions { get; set; } = new();
        public List<string> Missions { get; set; } = new();

        public List<FacilityMemberDTO> Members { get; set; } = new();
        public List<DynamicSectionDTO> DynamicSections { get; set; } = new();
    }

    public class FacilityMemberDTO
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string? Position { get; set; }
        public string? Department { get; set; }
        public string? ImagePath { get; set; }
        public int DisplayOrder { get; set; }
        // Email/Contact intentionally excluded from public API
    }

    // ── STUDENT CLUB LIST (for /api/clubs) ─────────────────
    public class StudentClubListDTO
    {
        public int Id { get; set; }
        public string Title { get; set; } = string.Empty;
        public string? CoverImage { get; set; } // first image, if any
        public int DisplayOrder { get; set; }
        public string? Link { get; set; } // resolved link if dynamic
    }

    // ── STUDENT CLUB DETAIL (for /api/clubs/{id}) ──────────
    public class StudentClubDetailDTO
    {
        public int Id { get; set; }
        public string Title { get; set; } = string.Empty;
        public string? About { get; set; }
        public string? BlogLink { get; set; }
        public string? Link { get; set; } // resolved internal link if dynamic

        public List<ClubImageDTO> Images { get; set; } = new();
        public List<ClubMemberDTO> Members { get; set; } = new();
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
}