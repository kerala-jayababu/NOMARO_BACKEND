using System.ComponentModel.DataAnnotations;

namespace Georgetown_Internationsl_Academy.API.Models
{
    public class LeavePassage
    {
        [Key]
        public int? IdLeavePassage { get; set; }
        public int IdEmployee { get; set; }
        public int IdFinancialYear { get; set; }
        public int IdSalaryMonth { get; set; }
        public string? Remarks { get; set; }
        public string ApprovalStatus { get; set; }
        public DateTime? CreatedDate {  get; set; }
    }
}
