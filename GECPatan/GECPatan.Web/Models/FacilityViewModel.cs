using GECPatan.Web.Models.Dtos;

namespace GECPatan.Web.Models
{
    public class FacilityListViewModel
    {
        public List<FacilityListDTO> Facilities { get; set; } = new();
        public string? ApiBaseUrl { get; set; }
    }

    public class FacilityViewModel
    {
        public int Id { get; set; }
        public string Title { get; set; } = string.Empty;
        public string? Tagline { get; set; }
        public string? About { get; set; }
        public string? TitleImagePath { get; set; }
        public string? BlogspotLink { get; set; }
        public List<string> BannerImages { get; set; } = new();
        public List<string> Visions { get; set; } = new();
        public List<string> Missions { get; set; } = new();
        public List<FacilityMemberDTO> Members { get; set; } = new();
        public List<DynamicSectionDTO> DynamicSections { get; set; } = new();

        public string? ApiBaseUrl { get; set; }
    }
}
