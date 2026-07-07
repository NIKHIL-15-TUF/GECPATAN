using GECPatan.Web.Models.Dtos;

namespace GECPatan.Web.Services
{
    public interface IHomeApiService
    {
        Task<SiteSettingsDTO?> GetSettingsAsync(CancellationToken ct = default);
    }
}
