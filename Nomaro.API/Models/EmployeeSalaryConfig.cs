using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Nomaro.API.Models
{
    public class EmployeeSalaryConfig
    {
        [Key]
        public int IdEmployeeSalaryConfig { get; set; }
        public int IdEmployee { get; set; }
        public DateTime ValidFrom { get; set; }
        public DateTime? ValidTo { get; set; }
        public int? IdSalaryTemplate { get; set; }
        public int? CreatedBy { get; set; }
        public DateTime? CreatedOn { get; set; }       
        public string? ApprovalStatus { get; set; }
        public bool? ActiveStatus { get; set; }
        public decimal? TotalEarnings { get; set; }
        public decimal? TotalDeductions { get; set; }
        public decimal? NetSalary { get; set; }
        public decimal? CTCAnnual { get; set; }
        public decimal? CTCMonthly { get; set; }
        public decimal? GrossMonthly { get; set; }
        public decimal? TotalEmployerContribution { get; set; }
        public string? RevisionReason { get; set; }
        public int? ApprovedBy { get; set; }
        public DateTime? ApprovedOn { get; set; }
        public int? ModifiedBy { get; set; }
        public DateTime? ModifiedOn { get; set; }

        [NotMapped]
        public virtual ICollection<EmployeeSalaryConfigDetails> EmployeeSalaryConfigDetails { get; set; }
    }
}

