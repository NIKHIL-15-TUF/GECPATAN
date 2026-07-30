using GECPatan.Web.Models.Dtos;
using System.Net.Http.Json;
using System.Text.Json;

namespace GECPatan.Web.Services
{
    // Register in Program.cs as:
    //   builder.Services.AddHttpClient<IDocumentApiService, DocumentApiService>(client =>
    //   {
    //       var baseUrl = builder.Configuration["Api:BaseUrl"]
    //           ?? throw new InvalidOperationException("Api:BaseUrl is not configured.");
    //       client.BaseAddress = new Uri(baseUrl);
    //       client.Timeout = TimeSpan.FromSeconds(30);
    //       client.DefaultRequestHeaders.Add("Accept", "application/json");
    //   });
    public class DocumentApiService : IDocumentApiService
    {
        private readonly HttpClient _http;
        private readonly ILogger<DocumentApiService> _logger;

        private static readonly JsonSerializerOptions JsonOptions = new()
        {
            PropertyNameCaseInsensitive = true
        };

        public DocumentApiService(HttpClient http, ILogger<DocumentApiService> logger)
        {
            _http = http;
            _logger = logger;
        }

        // GET /api/documents/categories
        public async Task<List<DocumentCategoryListDTO>> GetCategoriesAsync(CancellationToken ct = default)
            => await GetAsync<List<DocumentCategoryListDTO>>("api/documents/categories", ct) ?? new();

        // GET /api/documents/{categoryId}
        public async Task<DocumentCategoryDetailDTO?> GetCategoryDetailAsync(int categoryId, CancellationToken ct = default)
            => await GetAsync<DocumentCategoryDetailDTO>($"api/documents/{categoryId}", ct);

        public async Task<List<MoUDocumentDTO>> GetMoUAsync(CancellationToken ct = default)
            => await GetAsync<List<MoUDocumentDTO>>("api/documents/mou", ct) ?? new();

        public async Task<List<SSIPDocumentDTO>> GetSSIPAsync(CancellationToken ct = default)
            => await GetAsync<List<SSIPDocumentDTO>>("api/documents/ssip", ct) ?? new();

        public async Task<List<TimetableDTO>> GetTimetablesAsync(CancellationToken ct = default)
            => await GetAsync<List<TimetableDTO>>("api/documents/timetable", ct) ?? new();

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
