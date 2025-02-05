using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Georgetown_Internationsl_Academy.API.DTO
{
    public class SalaryTemplateDetailDto
    {
        public int? IdSalaryTemplateDetail { get; set; }
        public int IdSalaryTemplate { get; set; }
        public int IdSalaryHead { get; set; }
        [ValidCalculationMethod]
        public string CalculationMethod { get; set; }
        public decimal? FixedAmount { get; set; }
        public int? PercentageOfIdSalaryHead { get; set; }
        public decimal? PercentageValue { get; set; }
        public string? CustomFormula { get; set; }
        public decimal FinalSalaryAmount { get; set; }
        public string? Remarks { get; set; }

        [NotMapped]
        public string SalaryHeadName { get; set; }
        [NotMapped]
        public string HeadType { get; set; }
        [NotMapped]
        public bool IsTaxable { get; set; }
        [NotMapped]
        public int? OrderNumber { get; set; }
      
    }

    public class ValidCalculationMethodAttribute : ValidationAttribute
    {
        private readonly string[] _validMethods = { "FORMULA", "PERCENTAGE", "FIXEDAMOUNT" };

        protected override ValidationResult IsValid(object? value, ValidationContext validationContext)
        {
            if (value is string calculationMethod && _validMethods.Contains(calculationMethod))
            {
                return ValidationResult.Success!;
            }

            return new ValidationResult($"Invalid Calculation Method. Allowed values are: {string.Join(", ", _validMethods)}.");
        }
    }
}
