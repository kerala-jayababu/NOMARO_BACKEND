    namespace Nomaro.API.DTO
    {
        public class MaternityLeaveSalaryDetailDto
        {
            public int? IdMaternityLeaveSalaryDetail { get; set; }
            public int? IdMaternityLeaveSalary { get; set; }
            public int? IdSalaryHead { get; set; }
            public string? SalaryHeadName { get; set; } = string.Empty;
            public string? SalaryHeadType { get; set; } = string.Empty;
            public decimal? Amount { get; set; }
            public decimal? AmountInUSD { get; set; }
            public int? CreatedBy { get; set; }
            public DateTime? CreatedDate { get; set; }
        }
    }

