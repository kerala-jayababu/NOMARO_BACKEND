using System.ComponentModel.DataAnnotations;

namespace Georgetown_Internationsl_Academy.API.Models
{
    public class MaternityLeaveSalaryEntity
    {
        [Key]
        public int? IdMaternityLeaveSalary { get; set; }
        public int IdEmployee { get; set; }
        public DateTime MaternityLeaveFrom { get; set; }
        public DateTime MaternityLeaveTo { get; set; }
        public decimal NetSalary { get; set; }
        public decimal MaternityLeaveSalary { get; set; }
    }
}
