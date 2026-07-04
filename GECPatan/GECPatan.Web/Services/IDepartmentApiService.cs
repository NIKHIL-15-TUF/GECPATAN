using GECPatan.Web.Models.Dtos;
namespace GECPatan.Web.Services
{
    public interface IDepartmentApiService
    {
        Task<List<DepartmentListDTO>> GetAllDepartmentsAsync(CancellationToken ct = default);
        Task<DepartmentDetailDTO?> GetDepartmentDetailAsync(int id, CancellationToken ct = default);
        Task<List<DeptFacultyDTO>> GetFacultyAsync(int id, CancellationToken ct = default);
        Task<List<DeptLabDTO>> GetLabsAsync(int id, CancellationToken ct = default);
        Task<List<DeptTimetableDTO>> GetTimetableAsync(int id, CancellationToken ct = default);
        Task<List<DeptIntakeDTO>> GetIntakeAsync(int id, CancellationToken ct = default);
        Task<List<DeptNoticeDTO>> GetNoticesAsync(int id, CancellationToken ct = default);
        Task<List<ActivityDTO>> GetActivitiesAsync(int id, CancellationToken ct = default);
        Task<List<AchievementDTO>> GetAchivementsAsync(int id, CancellationToken ct = default);
    }
}
