using System.ComponentModel.DataAnnotations;

namespace Georgetown_Internationsl_Academy.API.Models
{
    public class BankBranches
    {
        [Key]
        public int IdBankBranches { get; set; }
        public int? IdBank { get; set; }
        public string? BranchName { get; set; }
        public string? BankAddress { get; set; }
        public string? PhoneNumber { get; set; }
        public string? ABARoutingNumber { get; set; }
        

    }
}
