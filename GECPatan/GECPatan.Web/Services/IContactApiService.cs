using GECPatan.Web.Models.Dtos;

namespace GECPatan.Web.Services
{
    public interface IContactApiService
    {
        Task<ContactSettingsDTO?> GetSettingsAsync(CancellationToken ct = default);

        /// <summary>
        /// Posts the form to the Api. Never throws — returns (false, a
        /// friendly message) on any transport/deserialization failure so the
        /// controller can always render a sensible result to the browser.
        /// </summary>
        Task<(bool Success, string Message)> SubmitAsync(ContactSubmitRequestDTO request, CancellationToken ct = default);
    }
}
