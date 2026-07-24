using GECPatan.Web.Models.Dtos;
namespace GECPatan.Web.Services
{
    public interface IFacilityApiService
    {
        Task<List<FacilityListDTO>> GetAllFacilitiesAsync(CancellationToken ct = default);
        Task<FacilityDetailDTO?> GetFacilityDetailAsync(int id, CancellationToken ct = default);
    }
}
