using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;

namespace Nomaro.API.Models
{
    public class EmployeeSalaryDetails
    {
        [Key]
        public int IdEmployeeSalaryDetail { get; set; }

        public int? IdEmployeeSalary { get; set; }

        public int? IdSalaryHead { get; set; }

        public string SalaryHeadName { get; set; }

        public string SalaryHeadType { get; set; } 

        public decimal? Amount { get; set; }

        public decimal? AmountInUSD { get; set; }
        public decimal? YTDAmount { get; set; }
        public decimal? YTDAmountUSD { get; set; }        

        public string? Remarks { get; set; }

        public int? OrderNumber { get; set; }

        public int? PercentageOfIdSalaryHead { get; set; }
    }
}

