using GECPatan.Web.Models.Dtos;
namespace GECPatan.Web.Services
{
    public interface IGalleryApiService
    {
        Task<List<GalleryImageDTO>> GetAllAsync(CancellationToken ct = default);
    }
}
