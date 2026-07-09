using GECPatan.Web.Models.Dtos;

namespace GECPatan.Web.Services
{
    public interface IFacultyApiService
    {
        Task<FacultyDetailDTO?> GetFacultyDetailAsync(int facultyId, CancellationToken ct = default);
    }
}