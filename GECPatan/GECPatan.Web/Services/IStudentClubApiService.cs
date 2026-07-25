using GECPatan.Web.Models.Dtos;

namespace GECPatan.Web.Services
{
    public interface IStudentClubApiService
    {
        Task<List<StudentClubListDTO>> GetAllClubsAsync(CancellationToken ct = default);
        Task<StudentClubDetailDTO?> GetClubDetailAsync(int id, CancellationToken ct = default);
        Task<List<ActivityDTO>> GetActivitiesAsync(int id, CancellationToken ct = default);
    }
}
