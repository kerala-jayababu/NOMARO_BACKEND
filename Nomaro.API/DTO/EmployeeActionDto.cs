using System;
using System.ComponentModel.DataAnnotations;

namespace Nomaro.API.DTO
{
    public class EmployeeActionDto
    {
        [Key]
        public int IdEmployeeAction { get; set; }
        public int IdEmployee { get; set; }

        public string? EmployeeCode { get; set; }
        public string? EmployeeName { get; set; }

        public string? DepartmentName { get; set; }
        public string? DesignationName { get; set; }

        public string? ActionType { get; set; }
        public string? ActionDescription { get; set; }
        public string? ActionSeverity { get; set; }
        public string? Remarks { get; set; }

        public DateTime? EffectiveFromDate { get; set; }
        public DateTime? EffectiveToDate { get; set; }

        public string? Status { get; set; }

        public int? CreatedBy { get; set; }
        public DateTime? CreatedDate { get; set; }

        public int? ApprovedBy { get; set; }
        public DateTime? ApprovedDate { get; set; }
    }
}
