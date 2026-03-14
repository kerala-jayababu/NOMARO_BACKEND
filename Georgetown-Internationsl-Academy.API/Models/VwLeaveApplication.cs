using System.ComponentModel.DataAnnotations;

namespace Georgetown_Internationsl_Academy.API.Models
{
    public class VwLeaveApplication
    {
        [Key]
        public int IdLeaveApplication { get; set; }
        public int IdEmployee { get; set; }
        public int IdDesignation { get; set; }
        public int IdDepartment { get; set; }
        public string EmployeeName { get; set; }
        public int IdLeaveType { get; set; }
        public string LeaveTypeName { get; set; }
        public string LeaveCode { get; set; }
        public DateTime FromDate { get; set; }
        public DateTime ToDate { get; set; }
        public int IdYear { get; set; }
        public string DepartmentName { get; set; }
        public string DesignationName { get; set; }
        public decimal TotalLeaveDays { get; set; }
    }
}
