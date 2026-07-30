using GECPatan.Web.Models.Dtos;

namespace GECPatan.Web.Services
{
    public interface IDocumentApiService
    {
        Task<List<DocumentCategoryListDTO>> GetCategoriesAsync(CancellationToken ct = default);
        Task<DocumentCategoryDetailDTO?> GetCategoryDetailAsync(int categoryId, CancellationToken ct = default);
        Task<List<MoUDocumentDTO>> GetMoUAsync(CancellationToken ct = default);
        Task<List<SSIPDocumentDTO>> GetSSIPAsync(CancellationToken ct = default);
        Task<List<TimetableDTO>> GetTimetablesAsync(CancellationToken ct = default);
    }
}
