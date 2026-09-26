using System.ComponentModel.DataAnnotations;

namespace Nomaro.API.Models
{
    public class BudgetCodeEntity
    {
        [Key]
        public int IdBudgetCode { get; set; }
        public string BudgetCode { get; set; }
        public string BudgetCodeName { get; set; }
    }
}

