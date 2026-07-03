using GECPatan.Web.Models.Dtos;
using System.Net.Http.Json;
using System.Text.Json;

namespace GECPatan.Web.Services
{
    // Register in Program.cs as:
    //   builder.Services.AddHttpClient<IDepartmentApiService, DepartmentApiService>(client =>
    //   {
    //       client.BaseAddress = new Uri(builder.Configuration["Api:BaseUrl"]!);
    //   });
    // and add to appsettings.json:
    //   "Api": { "BaseUrl": "https://localhost:5001/" }
    public class DepartmentApiService : IDepartmentApiService
    {
        private readonly HttpClient _http;
        private readonly ILogger<DepartmentApiService> _logger;

        private static readonly JsonSerializerOptions JsonOptions = new()
        {
            PropertyNameCaseInsensitive = true
        };

        public DepartmentApiService(HttpClient http, ILogger<DepartmentApiService> logger)
        {
            _http = http;
            _logger = logger;
        }

        public async Task<List<DepartmentListDTO>> GetAllDepartmentsAsync(CancellationToken ct = default)
            => await GetAsync<List<DepartmentListDTO>>("api/departments", ct) ?? new();

        public async Task<DepartmentDetailDTO?> GetDepartmentDetailAsync(int id, CancellationToken ct = default)
            => await GetAsync<DepartmentDetailDTO>($"api/departments/{id}", ct);

        public async Task<List<DeptFacultyDTO>> GetFacultyAsync(int id, CancellationToken ct = default)
            => await GetAsync<List<DeptFacultyDTO>>($"api/departments/{id}/faculty", ct) ?? new();

        public async Task<List<DeptLabDTO>> GetLabsAsync(int id, CancellationToken ct = default)
            => await GetAsync<List<DeptLabDTO>>($"api/departments/{id}/labs", ct) ?? new();

        public async Task<List<DeptTimetableDTO>> GetTimetableAsync(int id, CancellationToken ct = default)
            => await GetAsync<List<DeptTimetableDTO>>($"api/departments/{id}/timetable", ct) ?? new();

        public async Task<List<DeptIntakeDTO>> GetIntakeAsync(int id, CancellationToken ct = default)
            => await GetAsync<List<DeptIntakeDTO>>($"api/departments/{id}/intake", ct) ?? new();

        public async Task<List<DeptNoticeDTO>> GetNoticesAsync(int id, CancellationToken ct = default)
            => await GetAsync<List<DeptNoticeDTO>>($"api/departments/{id}/notices", ct) ?? new();

        public async Task<List<ActivityDTO>> GetActivitiesAsync(int id, CancellationToken ct = default)
            => await GetAsync<List<ActivityDTO>>($"api/departments/{id}/activities", ct) ?? new();

        private async Task<T?> GetAsync<T>(string url, CancellationToken ct)
        {
            try
            {
                var response = await _http.GetAsync(url, ct);

                // 404 (e.g. department not found) is an expected outcome, not a failure to throw on.
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