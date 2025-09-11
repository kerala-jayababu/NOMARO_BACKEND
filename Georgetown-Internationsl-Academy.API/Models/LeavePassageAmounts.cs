using System.ComponentModel.DataAnnotations;

namespace Georgetown_Internationsl_Academy.API.Models
{
    public class LeavePassageAmounts
    {
        [Key]
        public int IdLeavePassageAmount { get; set; }   
        public int? IdFinancialYear { get; set; }       
        public int IdEmployee { get; set; }             
        public DateTime? DateFrom { get; set; }         
        public DateTime? DateTo { get; set; }          
        public decimal LeavePassageAmount { get; set; }
    }
}
