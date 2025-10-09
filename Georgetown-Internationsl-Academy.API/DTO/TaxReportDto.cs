using System;
using System.Collections.Generic;

namespace Georgetown_Internationsl_Academy.API.DTO
{
  
        public class TaxReportDto
        {
        public int IdEmployee { get; set; }
        public string? EmployeeName { get; set; }
        public string? TINNumber { get; set; }
        public string? NISNumber { get; set; }
        public string? EmployeeNumber { get; set; }

        // Employer/Employee header fields used by PDF template
        public string? EmployerTIN { get; set; }
        public string? EmployerName { get; set; }
        public string? EmployerAddress { get; set; }
        public string? EmployeeAddress { get; set; }
        public string? EmployeeTIN { get; set; }

        // Period fields used in PDF
        public DateTime? PeriodFrom { get; set; }
        public DateTime? PeriodTo { get; set; }

        public decimal? SalaryOrWages { get; set; }
        public decimal? RentFreeQuartersOrHouseAllowance { get; set; }
        public decimal? BonusAndProfitShare { get; set; }
        public decimal? Overtime { get; set; }
        public decimal? BoardAndLodge { get; set; }
        public decimal? Fees { get; set; }
        public decimal? OtherAllowances { get; set; }
        public decimal? TotalIncome { get; set; }
        public decimal? TotalTaxableIncome { get; set; }
        public decimal? NISContribution { get; set; }
        public decimal? MedicalAndLifeInsurancePremiums { get; set; }
        public decimal? IncomeTaxDeducted { get; set; }

        // List of salary heads with YTD Amounts
        public List<TaxReportDetailDto> SalaryHeadDetails { get; set; } = new();

    
    }

    public class TaxReportDetailDto
    {
        public string SalaryHeadName { get; set; }
        public decimal YTDAmount { get; set; }
    }

   
}
