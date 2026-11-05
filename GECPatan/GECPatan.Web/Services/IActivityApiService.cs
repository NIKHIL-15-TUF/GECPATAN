using GECPatan.Web.Models.Dtos;

namespace GECPatan.Web.Services
{
    public interface IActivityApiService
    {
        Task<(List<ActivityDTO> Data, int Total)> GetActivitiesAsync(
            int? year = null,
            int page = 1,
            int pageSize = 20,
            CancellationToken ct = default);

        Task<ActivityDTO?> GetActivityByIdAsync(
            int id,
            CancellationToken ct = default);

        Task<List<int>> GetYearsAsync(
            CancellationToken ct = default);
    }
}