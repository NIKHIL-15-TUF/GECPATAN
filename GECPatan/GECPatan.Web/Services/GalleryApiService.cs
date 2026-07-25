using GECPatan.Web.Models.Dtos;
using System.Text.Json;

namespace GECPatan.Web.Services
{
    // Register in Program.cs as:
    //   builder.Services.AddHttpClient<IGalleryApiService, GalleryApiService>(client =>
    //   {
    //       var baseUrl = builder.Configuration["Api:BaseUrl"]
    //           ?? throw new InvalidOperationException("Api:BaseUrl is not configured.");
    //       client.BaseAddress = new Uri(baseUrl);
    //       client.Timeout = TimeSpan.FromSeconds(30);
    //       client.DefaultRequestHeaders.Add("Accept", "application/json");
    //   });
    public class GalleryApiService : IGalleryApiService
    {
        private readonly HttpClient _http;
        private readonly ILogger<GalleryApiService> _logger;

        private static readonly JsonSerializerOptions JsonOptions = new()
        {
            PropertyNameCaseInsensitive = true
        };

        public GalleryApiService(HttpClient http, ILogger<GalleryApiService> logger)
        {
            _http = http;
            _logger = logger;
        }

        // ASSUMPTION: endpoint is GET /api/gallery returning a FLAT image list.
        // If your API returns the grouped-by-category shape instead, this needs
        // to change (both the URL and the DTO being deserialized).
        public async Task<List<GalleryImageDTO>> GetAllAsync(CancellationToken ct = default)
            => await GetAsync<List<GalleryImageDTO>>("api/gallery", ct) ?? new();

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
