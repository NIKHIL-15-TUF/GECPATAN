using GECPatan.Web.Models.Dtos;
using System.Net.Http.Json;
using System.Text.Json;

namespace GECPatan.Web.Services
{
    // Register in Program.cs as:
    //   builder.Services.AddHttpClient<IResearchGrantApiService, ResearchGrantApiService>(client =>
    //   {
    //       var baseUrl = builder.Configuration["Api:BaseUrl"]
    //           ?? throw new InvalidOperationException("Api:BaseUrl is not configured.");
    //       client.BaseAddress = new Uri(baseUrl);
    //       client.Timeout = TimeSpan.FromSeconds(30);
    //       client.DefaultRequestHeaders.Add("Accept", "application/json");
    //   });
    public class ResearchGrantApiService : IResearchGrantApiService
    {
        private readonly HttpClient _http;
        private readonly ILogger<ResearchGrantApiService> _logger;

        private static readonly JsonSerializerOptions JsonOptions = new()
        {
            PropertyNameCaseInsensitive = true
        };

        public ResearchGrantApiService(HttpClient http, ILogger<ResearchGrantApiService> logger)
        {
            _http = http;
            _logger = logger;
        }

        // ASSUMPTION: endpoint is GET /api/researchgrants — confirm/correct.
        public async Task<List<ResearchGrantDTO>> GetAllAsync(CancellationToken ct = default)
            => await GetAsync<List<ResearchGrantDTO>>("api/academics/research", ct) ?? new();

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
