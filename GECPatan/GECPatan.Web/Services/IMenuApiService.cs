using GECPatan.Web.Models.Dtos;

namespace GECPatan.Web.Services
{
    public interface IMenuApiService
    {
        Task<List<MenuItemDTO>> GetMainMenuAsync(CancellationToken ct = default);
        Task<List<MenuItemDTO>> GetFooterMenuAsync(CancellationToken ct = default);
        Task<List<MenuItemDTO>> GetTopMenuAsync(CancellationToken ct = default);
    }
}
