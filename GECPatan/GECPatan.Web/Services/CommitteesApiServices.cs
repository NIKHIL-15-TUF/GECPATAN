using GECPatan.Web.Models.Dtos;
using System.Net.Http.Json;
using System.Text.Json;

namespace GECPatan.Web.Services
{
    // Register in Program.cs as:
    //   builder.Services.AddHttpClient<ICommitteeApiService, CommitteeApiService>(client =>
    //   {
    //       client.BaseAddress = new Uri(builder.Configuration["Api:BaseUrl"]!);
    //   });
    //
    // ASSUMPTION: routes are GET /api/committees/{id} and
    // GET /api/committees/{id}/activities, matching the pattern of
    // /api/departments/{id}[/activities]. Update the URLs below if your
    // actual routes differ -- that's the only thing that needs to change.
    public class CommitteeApiService : ICommitteeApiService
    {
        private readonly HttpClient _http;
        private readonly ILogger<CommitteeApiService> _logger;

        private static readonly JsonSerializerOptions JsonOptions = new()
        {
            PropertyNameCaseInsensitive = true
        };

        public CommitteeApiService(HttpClient http, ILogger<CommitteeApiService> logger)
        {
            _http = http;
            _logger = logger;
        }

        public Task<CommitteeDetailDTO?> GetCommitteeDetailAsync(int id, CancellationToken ct = default)
            => GetAsync<CommitteeDetailDTO>($"api/committees/{id}", ct);

        public async Task<List<ActivityDTO>> GetActivitiesAsync(int id, CancellationToken ct = default)
            => await GetAsync<List<ActivityDTO>>($"api/committees/{id}/activities", ct) ?? new();

        private async Task<T?> GetAsync<T>(string url, CancellationToken ct)
        {
            try
            {
                var response = await _http.GetAsync(url, ct);

                // 404 (e.g. no activities endpoint yet, or committee not found)
                // is an expected outcome here, not a failure to throw on.
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