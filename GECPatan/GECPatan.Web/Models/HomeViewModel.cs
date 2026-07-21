using GECPatan.Web.Models.Dtos;

namespace GECPatan.Web.Models
{
    public class HomeViewModel
    {
        // Uploaded images/files (slider/testimonial/recruiter/principal/
        // activity/marquee) are served by GECPatan.Api's own static file
        // middleware, not by this Web app -- see ResolveApiFileUrl() in the
        // view, same reasoning as DepartmentViewModel.ApiBaseUrl.
        public string ApiBaseUrl { get; set; } = string.Empty;

        public List<SliderDTO> Sliders { get; set; } = new();

        // Horizontal ticker below the slider (Marquee items with
        // HorizontalMarquee == true only -- API already filters this).
        public List<MarqueeDTO> Marquee { get; set; } = new();

        // Vertical "UPDATES" list (Marquee items with HorizontalMarquee ==
        // false, merged with NewsItem items flagged ShowInMarquee == true --
        // API already merges this via GET /api/home/updates).
        public List<UpdateItemDTO> Updates { get; set; } = new();

        public List<TestimonialDTO> Testimonials { get; set; } = new();
        public List<TopRecruiterDTO> TopRecruiters { get; set; } = new();
        public List<HomeActivityDTO> Activities { get; set; } = new();
        public List<DepartmentListDTO> Departments { get; set; } = new();

        public HomeStatsDTO? Stats { get; set; }
        public SiteSettingsDTO? Settings { get; set; }
        public PrincipalMessageDTO? Principal { get; set; }
    }
}
