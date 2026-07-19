using GECPatan.Web.Models.Dtos;

namespace GECPatan.Web.Services
{
    public interface ICommitteeApiService
    {
        Task<CommitteeDetailDTO?> GetCommitteeDetailAsync(int id, CancellationToken ct = default);

        // ASSUMPTION: mirrors the Department pattern (GET /api/departments/{id}/activities).
        // Confirm the actual route once you have it; failures here degrade to
        // an empty list rather than breaking the page (see CommitteeApiService).
        Task<List<ActivityDTO>> GetActivitiesAsync(int id, CancellationToken ct = default);
    }
}
