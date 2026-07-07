using GECPatan.Web.Models.Dtos;
using System.Net.Http.Json;
using System.Text.Json;

namespace GECPatan.Web.Services
{
    // Register in Program.cs as:
    //   builder.Services.AddHttpClient<IMenuApiService, MenuApiService>(client =>
    //   {
    //       client.BaseAddress = new Uri(builder.Configuration["Api:BaseUrl"]!);
    //   });
    public class MenuApiService : IMenuApiService
    {
        private readonly HttpClient _http;
        private readonly ILogger<MenuApiService> _logger;

        private static readonly JsonSerializerOptions JsonOptions = new()
        {
            PropertyNameCaseInsensitive = true
        };

        public MenuApiService(HttpClient http, ILogger<MenuApiService> logger)
        {
            _http = http;
            _logger = logger;
        }

        public async Task<List<MenuItemDTO>> GetMainMenuAsync(CancellationToken ct = default)
            => await GetAsync<List<MenuItemDTO>>("api/menu/main", ct) ?? new();

        public async Task<List<MenuItemDTO>> GetFooterMenuAsync(CancellationToken ct = default)
            => await GetAsync<List<MenuItemDTO>>("api/menu/footer", ct) ?? new();

        public async Task<List<MenuItemDTO>> GetTopMenuAsync(CancellationToken ct = default)
            => await GetAsync<List<MenuItemDTO>>("api/menu/top", ct) ?? new();

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
