using GECPatan.Web.Models.Dtos;

namespace GECPatan.Web.Services
{
    public interface ITenderApiService
    {
        Task<List<TenderGroupDTO>> GetAllAsync(CancellationToken ct = default);
    }
}
