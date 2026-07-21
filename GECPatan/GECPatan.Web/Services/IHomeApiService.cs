using GECPatan.Web.Models.Dtos;

namespace GECPatan.Web.Services
{
    public interface IHomeApiService
    {
        Task<SiteSettingsDTO?> GetSettingsAsync(CancellationToken ct = default);

        // GET /api/home/slider
        Task<List<SliderDTO>> GetSlidersAsync(CancellationToken ct = default);

        // GET /api/home/marquee -- horizontal ticker below the slider ONLY
        // (Admin > Marquee items with HorizontalMarquee == true).
        Task<List<MarqueeDTO>> GetMarqueeAsync(CancellationToken ct = default);

        // GET /api/home/updates?take=20 -- vertical "UPDATES" list. Merges
        // Marquee items with HorizontalMarquee == false and NewsItem items
        // with ShowInMarquee == true.
        Task<List<UpdateItemDTO>> GetUpdatesAsync(int take = 20, CancellationToken ct = default);

        // GET /api/home/testimonials
        Task<List<TestimonialDTO>> GetTestimonialsAsync(CancellationToken ct = default);

        // GET /api/home/toprecruiters
        Task<List<TopRecruiterDTO>> GetTopRecruitersAsync(CancellationToken ct = default);

        // GET /api/home/news?take=5 -- unfiltered latest news; not used by
        // the homepage's Updates section anymore (see GetUpdatesAsync), kept
        // for any other "latest news" use elsewhere.
        Task<List<HomeNewsDTO>> GetLatestNewsAsync(int take = 5, CancellationToken ct = default);

        // GET /api/home/stats
        Task<HomeStatsDTO?> GetStatsAsync(CancellationToken ct = default);

        // GET /api/home/principal
        Task<PrincipalMessageDTO?> GetPrincipalMessageAsync(CancellationToken ct = default);

        // GET /api/home/activities?take=10
        Task<List<HomeActivityDTO>> GetLatestActivitiesAsync(int take = 10, CancellationToken ct = default);
    }
}
