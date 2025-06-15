using Georgetown_Internationsl_Academy.API.DTO.Time___Attendance.Shift;

namespace Georgetown_Internationsl_Academy.API.Services.Interface.Time___Attendance.Shift
{
    public interface IShiftAssignmentService
    {
        Task<List<ShiftAssignmentDto>> GetShiftAssignmentsByShiftAsync(int idShift);
        Task<List<ShiftAssignmentDto>> ManageShiftAssignmentsAsync(List<ShiftAssignmentDto> assignments);
    }
}
