using System.ComponentModel.DataAnnotations.Schema;

namespace Nomaro.API.DTO
{
    public class EmployeeSalaryConfigDto
    {
        public int? IdEmployeeSalaryConfig { get; set; }
        public int IdEmployee { get; set; }
        public DateTime? ValidFrom { get; set; }
        public DateTime? ValidTo { get; set; }
        public int? IdSalaryTemplate { get; set; }
        public int CreatedBy { get; set; }
        public DateTime CreatedOn { get; set; }
        public int? ApprovedBy { get; set; }
        public DateTime? ApprovedDate { get; set; }
        public DateTime? ApprovedOn { get; set; }
        public int? ModifiedBy { get; set; }
        public DateTime? ModifiedOn { get; set; }
        public string? RevisionReason { get; set; }
        public string? ApprovalStatus { get; set; }
        public bool? ActiveStatus { get; set; }
        public decimal? TotalEarnings { get; set; }
        public decimal? TotalDeductions { get; set; }
        public decimal? NetSalary { get; set; }
        public decimal? CTCAnnual { get; set; }
        public decimal? CTCMonthly { get; set; }
        public decimal? GrossMonthly { get; set; }
        public decimal? TotalEmployerContribution { get; set; }

        /// <summary>Non-blocking warnings returned after saving (e.g. salary already generated for the month).</summary>
        [NotMapped]
        public List<string>? Warnings { get; set; }

        [NotMapped]
        public string? EmployeeCode { get; set; }
        [NotMapped]
        public string? EmployeeName { get; set; }
        [NotMapped]
        public int? IdDesignation { get; set; }
        [NotMapped]
        public string? DesignationName { get; set; }
        [NotMapped]
        public int? IdDepartment { get; set; }
        [NotMapped]
        public string? DepartmentName { get; set; }
        [NotMapped]
        public DateTime? JoiningDate { get; set; }
        [NotMapped]
        public string? Gender { get; set; }
        [NotMapped]
        public string? EmailID { get; set; }
        [NotMapped]
        public string? PhoneNumber1 { get; set; }
        [NotMapped]
        public string? PhoneNumber2 { get; set; }
        [NotMapped]
        public string? CurrentStatus { get; set; }

        [NotMapped]
        public string? CreatedByValue { get; set; }

        [NotMapped]
        public string? OverTimeAllowedStatus { get; set; }
        public int? ApprovedCount { get; set; }
        public int? NotApprovedCount { get; set; }
        public int? submittedCount { get; set; }
        public int? rejectedCount { get; set; }
        public int? notConfiguredCount { get; set; }

        public List<EmployeeSalaryConfigDetailsDto>? EmployeeSalaryConfigDetails { get; set; }



    }
}

