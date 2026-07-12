using GECPatan.Web.Models.Dtos;

namespace GECPatan.Web.Services
{
    public interface IAchievementApiService
    {
        // GET /api/achievements  (optional filters: deptId, committeeId, type)
        // type: 1=Academic, 2=Sports, 3=Cultural, 4=NSS, 5=Other
        Task<List<AchievementDTO>> GetAllAsync(
            int? deptId = null,
            int? committeeId = null,
            int? type = null,
            CancellationToken ct = default);
    }
}
