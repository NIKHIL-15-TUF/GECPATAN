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
        private static readonly JsonSerializerOptions JsonOptions = new()
        {
            PropertyNameCaseInsensitive = true
        };

        public DepartmentApiService(HttpClient http)
        {
            _http = http;
        }

        public async Task<DepartmentDetailDTO?> GetDepartmentDetailAsync(int id, CancellationToken ct = default)
        {
            var result = await GetAsync<DepartmentDetailDTO>($"api/departments/{id}", ct);
            return result;
        }

        public async Task<List<DeptFacultyDTO>> GetFacultyAsync(int id, CancellationToken ct = default)
            => await GetAsync<List<DeptFacultyDTO>>($"api/departments/{id}/faculty", ct) ?? new();

        public async Task<List<DeptLabDTO>> GetLabsAsync(int id, CancellationToken ct = default)
            => await GetAsync<List<DeptLabDTO>>($"api/departments/{id}/labs", ct) ?? new();

        public async Task<List<DeptTimetableDTO>> GetTimetableAsync(int id, CancellationToken ct = default)
            => await GetAsync<List<DeptTimetableDTO>>($"api/departments/{id}/timetable", ct) ?? new();

        public async Task<List<DeptNoticeDTO>> GetNoticesAsync(int id, CancellationToken ct = default)
            => await GetAsync<List<DeptNoticeDTO>>($"api/departments/{id}/notices", ct) ?? new();

        public async Task<List<ActivityDTO>> GetActivitiesAsync(int id, CancellationToken ct = default)
            => await GetAsync<List<ActivityDTO>>($"api/departments/{id}/activities", ct) ?? new();

        private async Task<T?> GetAsync<T>(string url, CancellationToken ct)
        {
            var response = await _http.GetAsync(url, ct);

            // 404 (e.g. department not found) is an expected outcome, not a failure to throw on.
            if (!response.IsSuccessStatusCode)
                return default;

            var envelope = await response.Content.ReadFromJsonAsync<ApiResponse<T>>(JsonOptions, ct);
            return envelope is { Success: true } ? envelope.Data : default;
        }
    }
}