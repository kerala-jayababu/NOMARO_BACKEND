using System.ComponentModel.DataAnnotations.Schema;

namespace Nomaro.API.DTO
{
    public class SalaryTemplateDto
    {
        public int? IdSalaryTemplate { get; set; }
        public string SalaryTemplateName { get; set; }
        public string? Description { get; set; }
        public int? CreatedBy { get; set; }
        public DateTime? CreatedOn { get; set; }
        public int? ModifiedBy { get; set; }
        public DateTime? ModifiedOn { get; set; }      
        public string? ApprovalStatus { get; set; }
        public bool ActiveStatus { get; set; }
        public decimal? TotalEarnings { get; set; }
        public decimal? TotalDeductions { get; set; }
        public decimal? NetSalary { get; set; }
        public decimal? GrossMonthly { get; set; }
        public decimal? TotalEmployerContribution { get; set; }
        public decimal? CTCMonthly { get; set; }
        public decimal? CTCAnnual { get; set; }
        public bool IsCTCBased { get; set; }
        public int? ApprovedBy { get; set; }
        public DateTime? ApprovedOn { get; set; }
        public List<SalaryTemplateDetailDto>? SalaryTemplateDetails { get; set; }

        [NotMapped]
        public string? CreatedByValue { get; set; }
    }
}

