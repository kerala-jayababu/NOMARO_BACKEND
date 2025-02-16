using System.ComponentModel.DataAnnotations.Schema;

namespace Georgetown_Internationsl_Academy.API.DTO
{
    public class SalaryAdjustmentDto
    {
        public int IdSalaryAdjustment { get; set; }
        public int IdEmployee { get; set; }
        public DateTime PayAdjustmentDate { get; set; }
        public string PayAdjustmentDetails { get; set; }
        public int AllocatingSalaryHead { get; set; }
        public char EarningOrDeduction { get; set; }
        public int AllocatingSalaryMonth { get; set; }
        public DateTime? AllocationSalaryMonthDate { get; set; }
        public decimal Amount { get; set; }
        public string? Remarks { get; set; }
        public Boolean? IsTaxable {  get; set; }

        [NotMapped]
        public string?	AllcoatingSalaryHeadName { get; set; }
        [NotMapped]
        public string? AllocatingSalaryMonthText { get; set; }        

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

    }
}
