using GECPatan.Web.Models.Dtos;
using System.Net.Http.Json;
using System.Text.Json;

namespace GECPatan.Web.Services
{
    // Register in Program.cs as:
    //   builder.Services.AddHttpClient<IHomeApiService, HomeApiService>(client =>
    //   {
    //       client.BaseAddress = new Uri(builder.Configuration["Api:BaseUrl"]!);
    //   });
    public class HomeApiService : IHomeApiService
    {
        private readonly HttpClient _http;
        private readonly ILogger<HomeApiService> _logger;

        private static readonly JsonSerializerOptions JsonOptions = new()
        {
            PropertyNameCaseInsensitive = true
        };

        public HomeApiService(HttpClient http, ILogger<HomeApiService> logger)
        {
            _http = http;
            _logger = logger;
        }

        public async Task<SiteSettingsDTO?> GetSettingsAsync(CancellationToken ct = default)
            => await GetAsync<SiteSettingsDTO>("api/home/settings", ct);

        public async Task<List<SliderDTO>> GetSlidersAsync(CancellationToken ct = default)
            => await GetAsync<List<SliderDTO>>("api/home/slider", ct) ?? new();

        public async Task<List<MarqueeDTO>> GetMarqueeAsync(CancellationToken ct = default)
            => await GetAsync<List<MarqueeDTO>>("api/home/marquee", ct) ?? new();

        public async Task<List<UpdateItemDTO>> GetUpdatesAsync(int take = 20, CancellationToken ct = default)
            => await GetAsync<List<UpdateItemDTO>>($"api/home/updates?take={take}", ct) ?? new();

        public async Task<List<TestimonialDTO>> GetTestimonialsAsync(CancellationToken ct = default)
            => await GetAsync<List<TestimonialDTO>>("api/home/testimonials", ct) ?? new();

        public async Task<List<TopRecruiterDTO>> GetTopRecruitersAsync(CancellationToken ct = default)
            => await GetAsync<List<TopRecruiterDTO>>("api/home/toprecruiters", ct) ?? new();

        public async Task<List<HomeNewsDTO>> GetLatestNewsAsync(int take = 5, CancellationToken ct = default)
            => await GetAsync<List<HomeNewsDTO>>($"api/home/news?take={take}", ct) ?? new();

        public async Task<HomeStatsDTO?> GetStatsAsync(CancellationToken ct = default)
            => await GetAsync<HomeStatsDTO>("api/home/stats", ct);

        public async Task<PrincipalMessageDTO?> GetPrincipalMessageAsync(CancellationToken ct = default)
            => await GetAsync<PrincipalMessageDTO>("api/home/principal", ct);

        public async Task<List<HomeActivityDTO>> GetLatestActivitiesAsync(int take = 10, CancellationToken ct = default)
            => await GetAsync<List<HomeActivityDTO>>($"api/home/activities?take={take}", ct) ?? new();

        private async Task<T?> GetAsync<T>(string url, CancellationToken ct)
        {
            try
            {
                var response = await _http.GetAsync(url, ct);

                if (!response.IsSuccessStatusCode)
                {
                    _logger.LogWarning("API call to {Url} returned {StatusCode}.", url, response.StatusCode);
                    return default;
                }

                var envelope = await response.Content.ReadFromJsonAsync<ApiResponse<T>>(JsonOptions, ct);

                if (envelope is null || !envelope.Success)
                {
                    _logger.LogWarning("API call to {Url} reported failure: {Message}", url, envelope?.Message);
                    return default;
                }

                return envelope.Data;
            }
            catch (HttpRequestException ex)
            {
                _logger.LogError(ex, "HTTP error calling {Url}", url);
                return default;
            }
            catch (TaskCanceledException ex) when (!ct.IsCancellationRequested)
            {
                _logger.LogError(ex, "Timed out calling {Url}", url);
                return default;
            }
        }
    }
}
