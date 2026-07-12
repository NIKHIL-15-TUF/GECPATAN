using GECPatan.Web.Models.Dtos;
using System.Net.Http.Json;
using System.Text.Json;

namespace GECPatan.Web.Services
{
    // Register in Program.cs as:
    //   builder.Services.AddHttpClient<IAchievementApiService, AchievementApiService>(client =>
    //   {
    //       client.BaseAddress = new Uri(builder.Configuration["Api:BaseUrl"]!);
    //       client.Timeout = TimeSpan.FromSeconds(30);
    //       client.DefaultRequestHeaders.Add("Accept", "application/json");
    //   });
    public class AchievementApiService : IAchievementApiService
    {
        private readonly HttpClient _http;
        private readonly ILogger<AchievementApiService> _logger;

        private static readonly JsonSerializerOptions JsonOptions = new()
        {
            PropertyNameCaseInsensitive = true
        };

        public AchievementApiService(HttpClient http, ILogger<AchievementApiService> logger)
        {
            _http = http;
            _logger = logger;
        }

        public async Task<List<AchievementDTO>> GetAllAsync(
            int? deptId = null,
            int? committeeId = null,
            int? type = null,
            CancellationToken ct = default)
        {
            var query = new List<string>();
            if (deptId.HasValue) query.Add($"deptId={deptId.Value}");
            if (committeeId.HasValue) query.Add($"committeeId={committeeId.Value}");
            if (type.HasValue) query.Add($"type={type.Value}");

            var url = "api/achievements";
            if (query.Count > 0)
            {
                url += "?" + string.Join("&", query);
            }

            return await GetAsync<List<AchievementDTO>>(url, ct) ?? new();
        }

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
