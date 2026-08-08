using GECPatan.Web.Models.Dtos;
using System.Text;
using System.Text.Json;

namespace GECPatan.Web.Services
{
    // Register in Program.cs as:
    //   builder.Services.AddHttpClient<IContactApiService, ContactApiService>(client =>
    //   {
    //       var baseUrl = builder.Configuration["Api:BaseUrl"]
    //           ?? throw new InvalidOperationException("Api:BaseUrl is not configured.");
    //       client.BaseAddress = new Uri(baseUrl);
    //       client.Timeout = TimeSpan.FromSeconds(30);
    //       client.DefaultRequestHeaders.Add("Accept", "application/json");
    //   });
    public class ContactApiService : IContactApiService
    {
        private readonly HttpClient _http;
        private readonly ILogger<ContactApiService> _logger;

        private static readonly JsonSerializerOptions JsonOptions = new()
        {
            PropertyNameCaseInsensitive = true
        };

        public ContactApiService(HttpClient http, ILogger<ContactApiService> logger)
        {
            _http = http;
            _logger = logger;
        }

        public async Task<ContactSettingsDTO?> GetSettingsAsync(CancellationToken ct = default)
        {
            try
            {
                var response = await _http.GetAsync("api/contact/settings", ct);

                if (!response.IsSuccessStatusCode)
                {
                    _logger.LogWarning("API call to api/contact/settings returned {StatusCode}.", response.StatusCode);
                    return null;
                }

                var envelope = await response.Content
                    .ReadFromJsonAsync<ApiResponse<ContactSettingsDTO>>(JsonOptions, ct);

                if (envelope is null || !envelope.Success)
                {
                    _logger.LogWarning("API call to api/contact/settings reported failure: {Message}", envelope?.Message);
                    return null;
                }

                return envelope.Data;
            }
            catch (HttpRequestException ex)
            {
                _logger.LogError(ex, "HTTP error calling api/contact/settings");
                return null;
            }
            catch (TaskCanceledException ex) when (!ct.IsCancellationRequested)
            {
                _logger.LogError(ex, "Timed out calling api/contact/settings");
                return null;
            }
        }

        public async Task<(bool Success, string Message)> SubmitAsync(
            ContactSubmitRequestDTO request, CancellationToken ct = default)
        {
            const string genericFailure = "Sorry, we couldn't submit your message right now. Please try again in a few minutes.";

            try
            {
                var json = JsonSerializer.Serialize(request);
                using var content = new StringContent(json, Encoding.UTF8, "application/json");

                var response = await _http.PostAsync("api/contact/submit", content, ct);

                var envelope = await response.Content
                    .ReadFromJsonAsync<ApiResponse<object>>(JsonOptions, ct);

                if (envelope is null)
                {
                    _logger.LogWarning("API call to api/contact/submit returned {StatusCode} with an unreadable body.", response.StatusCode);
                    return (false, genericFailure);
                }

                // On both success and validation/rate-limit failure, the Api's
                // configured message is exactly what should reach the browser —
                // it already carries the admin-editable Success/Failure copy,
                // or a specific validation error, so it's returned as-is.
                return (envelope.Success, envelope.Message ?? (envelope.Success ? "Thank you." : genericFailure));
            }
            catch (HttpRequestException ex)
            {
                _logger.LogError(ex, "HTTP error calling api/contact/submit");
                return (false, genericFailure);
            }
            catch (TaskCanceledException ex) when (!ct.IsCancellationRequested)
            {
                _logger.LogError(ex, "Timed out calling api/contact/submit");
                return (false, genericFailure);
            }
        }
    }
}
