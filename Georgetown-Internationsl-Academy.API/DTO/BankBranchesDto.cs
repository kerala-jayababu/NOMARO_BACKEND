using System.ComponentModel.DataAnnotations.Schema;

namespace Georgetown_Internationsl_Academy.API.DTO
{
    public class BankBranchesDto
    {
        public int IdBankBranches { get; set; }
        public int? IdBank { get; set; }
        public string? BranchName { get; set; }
        public string? BankAddress { get; set; }
        public string? PhoneNumber { get; set; }
        public string? ABARoutingNumber { get; set; }

        [NotMapped]
        public string? BankName { get; set; }

    }
}
