using Nomaro.API.DTO.Time___Attendance.Shift;

namespace Nomaro.API.Services.Interface.Time___Attendance.Shift
{
    public interface IShiftAssignmentService
    {
        Task<List<ShiftAssignmentDto>> GetShiftAssignmentsByShiftAsync(int idShift);
        Task<List<ShiftAssignmentDto>> ManageShiftAssignmentsAsync(List<ShiftAssignmentDto> assignments);
        Task<bool> DeleteShiftAssignment(int IdShiftAssignment, int IdEmployee, int IdLoginnedEmployee);
        Task<bool> DeleteShiftAssignmentOfASchedule(int IdShiftSchedule, DateTime ShiftStartDateTime, int loggedInEmployeeId);
        Task<List<ShiftAssignmentDto>> CopyShiftAssignmentsByDateAsync(int idShiftSchedule, DateTime sourceDate, DateTime targetDate);
    }

}

