using Nomaro.API.DTO;

namespace Nomaro.API.Services.Interface
{
    public interface IGradeService
    {
        Task<IEnumerable<GradeDto>> GetGrades(bool? isActive);
        Task<GradeDto?> GetGradeById(int idGrade);
        Task<bool> IsGradeCodeExists(string gradeCode, int idGrade);
        Task<bool> AddOrUpdateGrade(GradeDto dto, int idLoggedInEmployee);
    }
}
