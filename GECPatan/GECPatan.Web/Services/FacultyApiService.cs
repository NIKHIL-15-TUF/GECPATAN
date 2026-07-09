using GECPatan.Web.Models.Dtos;
using System.Net.Http.Json;
using System.Text.Json;

namespace GECPatan.Web.Services
{
    public class FacultyApiService : IFacultyApiService
    {
        private readonly HttpClient _http;
        private readonly ILogger<FacultyApiService> _logger;

        private static readonly JsonSerializerOptions JsonOptions = new()
        {
            PropertyNameCaseInsensitive = true
        };

        public FacultyApiService(HttpClient http, ILogger<FacultyApiService> logger)
        {
            _http = http;
            _logger = logger;
        }

        public async Task<FacultyDetailDTO?> GetFacultyDetailAsync(int facultyId, CancellationToken ct = default)
        {
            var url = $"api/faculty/{facultyId}";

            try
            {
                var response = await _http.GetAsync(url, ct);

                if (!response.IsSuccessStatusCode)
                {
                    _logger.LogWarning("API call to {Url} returned {StatusCode}.", url, response.StatusCode);
                    return null;
                }

                var envelope = await response.Content.ReadFromJsonAsync<ApiResponse<FacultyDetailDTO>>(JsonOptions, ct);

                if (envelope is null || !envelope.Success)
                {
                    _logger.LogWarning("API call to {Url} reported failure: {Message}", url, envelope?.Message);
                    return null;
                }

                return envelope.Data;
            }
            catch (HttpRequestException ex)
            {
                _logger.LogError(ex, "HTTP error calling {Url}", url);
                return null;
            }
            catch (TaskCanceledException ex) when (!ct.IsCancellationRequested)
            {
                _logger.LogError(ex, "Timed out calling {Url}", url);
                return null;
            }
        }
    }
}