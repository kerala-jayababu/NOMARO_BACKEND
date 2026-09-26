using System.ComponentModel.DataAnnotations.Schema;

namespace Nomaro.API.DTO.Time___Attendance.Shift
{
    public class ShiftEmployeeDto
    {
        public int? IdShiftEmployee { get; set; }
        public int IdShift { get; set; }
        public int IdEmployee { get; set; }
        [NotMapped] public string EmployeeCode { get; set; } = string.Empty;
        [NotMapped] public string EmployeeName { get; set; } = string.Empty;
        [NotMapped] public int IdDepartment { get; set; }
        [NotMapped] public int IdDesignation { get; set; }
        [NotMapped] public string Department { get; set; } = string.Empty;
        [NotMapped] public string Designation { get; set; } = string.Empty;
        [NotMapped] public string ShiftName { get; set; } = string.Empty;
    }
}

