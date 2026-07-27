using GECPatan.Web.Models.Dtos;
namespace GECPatan.Web.Services
{
    public interface IResearchGrantApiService
    {
        Task<List<ResearchGrantDTO>> GetAllAsync(CancellationToken ct = default);
    }
}
