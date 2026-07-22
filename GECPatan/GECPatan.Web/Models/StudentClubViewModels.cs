using GECPatan.Web.Models.Dtos;

namespace GECPatan.Web.Models
{
    // Mirrors the shape the old StudentClubs.cshtml expected from the
    // "Student" model (Title/Titleimage/Clubs), but populated from
    // GET /api/clubs instead of ~/Data/Facilities/StudentClub.json.
    public class StudentClubsViewModel
    {
        public string ApiBaseUrl { get; set; } = string.Empty;

        public string Title { get; set; } = "Student Clubs";
        public string? TitleImage { get; set; }

        public List<StudentClubListDTO> Clubs { get; set; } = new();
    }

    // Mirrors the shape the old ClubDetails.cshtml expected from the
    // "Club" model, but populated from GET /api/clubs/{id} and
    // GET /api/clubs/{id}/activities instead of StudentClub.json / Activities.json.
    public class ClubDetailsViewModel
    {
        public string ApiBaseUrl { get; set; } = string.Empty;

        public int Id { get; set; }
        public string Title { get; set; } = string.Empty;
        public string? About { get; set; }
        public string? BlogLink { get; set; }
        public string? CoverImage { get; set; } // first image, if any

        public List<ClubImageDTO> Images { get; set; } = new();
        public List<ClubMemberDTO> Members { get; set; } = new();
        public List<string> SubObjectives { get; set; } = new();
        public List<DynamicSectionDTO> DynamicSections { get; set; } = new();

        public List<ActivityDTO> Activities { get; set; } = new();
    }
}
