using GECPatan.Web.Models.Dtos;
using System.Net.Http.Json;
using System.Text.Json;

namespace GECPatan.Web.Services
{
    // Register in Program.cs as:
    //   builder.Services.AddHttpClient<IStudentClubApiService, StudentClubApiService>(client =>
    //   {
    //       client.BaseAddress = new Uri(builder.Configuration["Api:BaseUrl"]!);
    //   });
    public class StudentClubApiService : IStudentClubApiService
    {
        private readonly HttpClient _http;
        private readonly ILogger<StudentClubApiService> _logger;

        private static readonly JsonSerializerOptions JsonOptions = new()
        {
            PropertyNameCaseInsensitive = true
        };

        public StudentClubApiService(HttpClient http, ILogger<StudentClubApiService> logger)
        {
            _http = http;
            _logger = logger;
        }

        public async Task<List<StudentClubListDTO>> GetAllClubsAsync(CancellationToken ct = default)
            => await GetAsync<List<StudentClubListDTO>>("api/clubs", ct) ?? new();

        public async Task<StudentClubDetailDTO?> GetClubDetailAsync(int id, CancellationToken ct = default)
            => await GetAsync<StudentClubDetailDTO>($"api/clubs/{id}", ct);

        public async Task<List<ActivityDTO>> GetActivitiesAsync(int id, CancellationToken ct = default)
            => await GetAsync<List<ActivityDTO>>($"api/clubs/{id}/activities", ct) ?? new();

        private async Task<T?> GetAsync<T>(string url, CancellationToken ct)
        {
            try
            {
                var response = await _http.GetAsync(url, ct);

                // 404 (e.g. club not found / not visible) is an expected outcome, not a failure to throw on.
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
