namespace GECPatan.Web.Models.Dtos
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
    }
}
