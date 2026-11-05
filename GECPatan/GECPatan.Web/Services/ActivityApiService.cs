using GECPatan.Web.Models.Dtos;
using System.Net.Http.Json;
using System.Text.Json;

namespace GECPatan.Web.Services
{
    public class ActivityApiService : IActivityApiService
    {
        private readonly HttpClient _http;
        private readonly ILogger<ActivityApiService> _logger;

        private static readonly JsonSerializerOptions JsonOptions = new()
        {
            PropertyNameCaseInsensitive = true
        };

        public ActivityApiService(
            HttpClient http,
            ILogger<ActivityApiService> logger)
        {
            _http = http;
            _logger = logger;
        }

        public async Task<(List<ActivityDTO> Data, int Total)>
            GetActivitiesAsync(
                int? year = null,
                int page = 1,
                int pageSize = 20,
                CancellationToken ct = default)
        {
            var url = $"api/activities?page={page}&pageSize={pageSize}";

            if (year.HasValue)
                url += $"&year={year.Value}";

            try
            {
                var response = await _http.GetAsync(url, ct);

                if (!response.IsSuccessStatusCode)
                {
                    _logger.LogWarning(
                        "Activity API returned {StatusCode}",
                        response.StatusCode);

                    return (new List<ActivityDTO>(), 0);
                }

                var envelope =
                    await response.Content.ReadFromJsonAsync<
                        ApiResponse<List<ActivityDTO>>>(
                        JsonOptions, ct);

                if (envelope == null || !envelope.Success)
                    return (new List<ActivityDTO>(), 0);

                return (
                    envelope.Data ?? new List<ActivityDTO>(),
                    envelope.Total ?? 0
                );
            }
            catch (HttpRequestException ex)
            {
                _logger.LogError(ex, "Error calling Activity API");
                return (new List<ActivityDTO>(), 0);
            }
        }

        public async Task<ActivityDTO?> GetActivityByIdAsync(
            int id,
            CancellationToken ct = default)
        {
            try
            {
                var response = await _http.GetAsync(
                    $"api/activities/{id}", ct);

                if (!response.IsSuccessStatusCode)
                    return null;

                var envelope =
                    await response.Content.ReadFromJsonAsync<
                        ApiResponse<ActivityDTO>>(
                        JsonOptions, ct);

                return envelope?.Success == true
                    ? envelope.Data
                    : null;
            }
            catch (HttpRequestException ex)
            {
                _logger.LogError(
                    ex, "Error loading activity {ActivityId}", id);

                return null;
            }
        }

        public async Task<List<int>> GetYearsAsync(
            CancellationToken ct = default)
        {
            try
            {
                var response = await _http.GetAsync(
                    "api/activities/years", ct);

                if (!response.IsSuccessStatusCode)
                    return new List<int>();

                var envelope =
                    await response.Content.ReadFromJsonAsync<
                        ApiResponse<List<int>>>(
                        JsonOptions, ct);

                return envelope?.Success == true
                    ? envelope.Data ?? new List<int>()
                    : new List<int>();
            }
            catch (HttpRequestException ex)
            {
                _logger.LogError(ex, "Error loading Activity years");
                return new List<int>();
            }
        }
    }
}