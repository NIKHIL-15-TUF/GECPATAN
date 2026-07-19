using GECPatan.Web.Models.Dtos;

namespace GECPatan.Web.Models
{
    public class HomeViewModel
    {
        // Uploaded images (slider/testimonial/recruiter/principal/activity
        // thumbnails) are served by GECPatan.Api's own static file
        // middleware, not by this Web app -- see ResolveApiFileUrl() in the
        // view, same reasoning as DepartmentViewModel.ApiBaseUrl.
        public string ApiBaseUrl { get; set; } = string.Empty;

        public List<SliderDTO> Sliders { get; set; } = new();
        public List<MarqueeDTO> Marquee { get; set; } = new();
        public List<TestimonialDTO> Testimonials { get; set; } = new();
        public List<TopRecruiterDTO> TopRecruiters { get; set; } = new();
        public List<HomeNewsDTO> News { get; set; } = new();
        public List<HomeActivityDTO> Activities { get; set; } = new();
        public List<DepartmentListDTO> Departments { get; set; } = new();

        public HomeStatsDTO? Stats { get; set; }
        public SiteSettingsDTO? Settings { get; set; }
        public PrincipalMessageDTO? Principal { get; set; }
    }
}
