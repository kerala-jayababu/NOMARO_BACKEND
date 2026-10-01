using Nomaro.API.Models;
using System.ComponentModel.DataAnnotations.Schema;

namespace Nomaro.API.DTO
{
    public class EmployeePayslipDto
    {

        public int IdEmployeeSalary { get; set; }
        public string EmployeeCode { get; set; }
        public string EmployeeName { get; set; }
        public string? EmailID { get; set; }
        public string Position { get; set; }
        public string Department { get; set; }
        public string Period { get; set; }
        public string PayslipGeneratedDate { get; set; }
        public List<EmployeeSalaryDetailsDto> Earnings { get; set; }
        public List<EmployeeSalaryDetailsDto> Deductions { get; set; }
        public List<BankRemittanceDto>? BankRemittance { get; set; }

        [NotMapped]
        public byte[]? logo { get; set; }

        [NotMapped]
        public string? logoType { get; set; }

        [NotMapped]
        public byte[]? stamp { get; set; }
        [NotMapped]
        public string? stampType { get; set; }

        [NotMapped]
        public float LogoHeightInPayslip { get; set; } = 50f;

        [NotMapped]
        public float StampHeightInPayslip { get; set; } = 75f;

        // ---------- Salary slip (India layout) ----------
        // Anything that cannot be filled is left null and printed blank.
        [NotMapped] public string? CompanyName { get; set; }
        [NotMapped] public string? PayslipColorPattern { get; set; }
        [NotMapped] public string? AuthorisedSignatoryName { get; set; }
        [NotMapped] public byte[]? AuthorisedSignatureImage { get; set; }
        [NotMapped] public string? CompanyAddress { get; set; }
        [NotMapped] public string? CompanyRegistrationNumber { get; set; }   // printed as CIN
        [NotMapped] public string? PayMonthText { get; set; }                // e.g. "August 2025"

        [NotMapped] public string? DateOfJoining { get; set; }
        [NotMapped] public string? EmploymentType { get; set; }
        [NotMapped] public string? Location { get; set; }
        [NotMapped] public string? PanNumber { get; set; }                   // Employees.TaxIdNumber
        [NotMapped] public string? UanNumber { get; set; }
        [NotMapped] public string? PfNumber { get; set; }
        [NotMapped] public string? EsiNumber { get; set; }                   // Employees.NationalIDNumber
        [NotMapped] public string? BankName { get; set; }
        [NotMapped] public string? BankAccountNumber { get; set; }           // masked, last 4 digits shown
        [NotMapped] public string? LopDays { get; set; }

        [NotMapped] public List<EmployeeSalaryDetailsDto>? EmployerContributions { get; set; }

        [NotMapped] public string? YtdPeriodText { get; set; }               // e.g. "APR 2025 - AUG 2025"
        [NotMapped] public decimal? TotalEarningsYtd { get; set; }
        [NotMapped] public decimal? TotalDeductionsYtd { get; set; }
        [NotMapped] public decimal? NetPayYtd { get; set; }
        [NotMapped] public decimal? EmployerPfYtd { get; set; }
        [NotMapped] public decimal? EmployerEpsYtd { get; set; }
        //public List<EmployeeSalaryDetailsDto> TaxDetails { get; set; }
    }
}

