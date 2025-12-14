using Georgetown_Internationsl_Academy.API.Models;
using System.ComponentModel.DataAnnotations.Schema;

namespace Georgetown_Internationsl_Academy.API.DTO
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
        //public List<EmployeeSalaryDetailsDto> TaxDetails { get; set; }
    }
}
