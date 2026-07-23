using GECPatan.Web.Models.Dtos;
using System.Net.Http.Json;
using System.Text.Json;

namespace GECPatan.Web.Services
{
    // Register in Program.cs as:
    //   builder.Services.AddHttpClient<IFacilityApiService, FacilityApiService>(client =>
    //   {
    //       var baseUrl = builder.Configuration["Api:BaseUrl"]
    //           ?? throw new InvalidOperationException("Api:BaseUrl is not configured.");
    //       client.BaseAddress = new Uri(baseUrl);
    //       client.Timeout = TimeSpan.FromSeconds(30);
    //       client.DefaultRequestHeaders.Add("Accept", "application/json");
    //   });
    public class FacilityApiService : IFacilityApiService
    {
        private readonly HttpClient _http;
        private readonly ILogger<FacilityApiService> _logger;

        private static readonly JsonSerializerOptions JsonOptions = new()
        {
            PropertyNameCaseInsensitive = true
        };

        public FacilityApiService(HttpClient http, ILogger<FacilityApiService> logger)
        {
            _http = http;
            _logger = logger;
        }

        public async Task<List<FacilityListDTO>> GetAllFacilitiesAsync(CancellationToken ct = default)
            => await GetAsync<List<FacilityListDTO>>("api/facilities", ct) ?? new();

        public async Task<FacilityDetailDTO?> GetFacilityDetailAsync(int id, CancellationToken ct = default)
            => await GetAsync<FacilityDetailDTO>($"api/facilities/{id}", ct);

        private async Task<T?> GetAsync<T>(string url, CancellationToken ct)
        {
            try
            {
                var response = await _http.GetAsync(url, ct);

                // 404 (e.g. facility not found) is an expected outcome, not a failure to throw on.
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