using GECPatan.Web.Models.Dtos;

namespace GECPatan.Web.Services
{
    public interface INewsApiService
    {
        // GET /api/news/letters
        Task<List<NewsLetterDTO>> GetLettersAsync(CancellationToken ct = default);
    }
}
