using System.ComponentModel.DataAnnotations;

namespace Georgetown_Internationsl_Academy.API.Models
{
    public class EmployeeSalaries
    {
        [Key]
        public int IdEmployeeSalary { get; set; }
        public int IdEmployee { get; set; }
        public int IdSalaryMonth { get; set; }
        public string SalaryMonthText { get; set; } = string.Empty;
        public DateTime GeneratedDate { get; set; }
        public decimal TotalEarnings { get; set; }
        public decimal TotalDeductions { get; set; }
        public decimal TaxableIncome { get; set; }
        public decimal TaxAmountAccounted { get; set; }
        public decimal TaxAmountDeducted { get; set; }
        public int? CreatedBy { get; set; }
        public DateTime? CreatedDate { get; set; }
        public int? ModifiedBy { get; set; }
        public DateTime? ModifiedDate { get; set; }
        public int? IdApprovedBy { get; set; }
        public DateTime? ApprovedDate { get; set; }
        public DateTime? EmailSentDate { get; set; }
        public string ApprovalStatus { get; set; } = string.Empty;
        public string? EmailStatus { get; set; }
    }
}
