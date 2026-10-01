namespace Nomaro.API.DTO
{
    public class SalaryHeadDto
    {
        public int? IdSalaryHead { get; set; }
        public string SalaryHeadCode { get; set; }
        public string SalaryHeadName { get; set; }
        public string HeadType { get; set; }
        public bool IsTaxable { get; set; }
        public bool IsActive { get; set; }
        public string? CalculationMethod { get; set; }
        public int? IdPercentageSalaryHead { get; set; }
        public decimal? PercentageValue { get; set; }
        public decimal? FixedValue { get; set; }
        public string CustomFormula { get; set; }        
        public int? OrderNumber { get; set; }
        public decimal? NonTaxableThreshold { get; set; }
        public string ? DisbursingMonths { get; set; }
        public string? StatutoryType { get; set; }
        public bool IsPartOfGross { get; set; }
        public bool IsPartOfCTC { get; set; }
        public bool IsPartOfPFWage { get; set; }
        public bool IsPartOfESIWage { get; set; }
        public bool IsPartOfGratuityWage { get; set; }
        public bool IsPartOfPTWage { get; set; }
        public bool IsProratedOnPaidDays { get; set; }
        public bool IsArrearHead { get; set; }
        public int? IdBaseSalaryHead { get; set; }
        public decimal? MinAmount { get; set; }
        public decimal? MaxAmount { get; set; }
        public decimal? WageCeiling { get; set; }
        public string? PayFrequency { get; set; }
        public int? CalcSequence { get; set; }
        public string? RoundingRule { get; set; }
        public bool ShowOnPayslip { get; set; }
    }

}

