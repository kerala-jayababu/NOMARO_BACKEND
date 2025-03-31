using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Georgetown_Internationsl_Academy.API.DTO
{
    public class LeavePassageDto
    {

        
        public int? IdLeavePassage { get; set; }
        public int IdEmployee { get; set; }
        public int IdFinancialYear { get; set; }
        public int IdSalaryMonth { get; set; }
        public string? Remarks { get; set; }
        public string? ApprovalStatus { get; set; }

        [NotMapped]
        public string EmployeeCode { get; set; } = string.Empty;
        [NotMapped]
        public string EmployeeName { get; set; } = string.Empty;
        [NotMapped]
        public string DepartmentName { get; set; } = string.Empty;
        [NotMapped]
        public string DesignationName { get; set; } = string.Empty;
        [NotMapped]
        public int IdDepartment { get; set; }
        [NotMapped]
        public int IdDesignation { get; set; }

        [NotMapped]
        public string SalaryMonthText { get; set; }

        [NotMapped]
        public DateTime FinancialYearFrom{ get; set; }
        public DateTime FinancialYearTo { get; set; }






    }
}
