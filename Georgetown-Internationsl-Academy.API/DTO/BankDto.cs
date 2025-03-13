using System.ComponentModel.DataAnnotations;

namespace Georgetown_Internationsl_Academy.API.DTO
{
    public class BankDto
    {
        [Key]
        public int IdBank { get; set; }
        public string BankName { get; set; }
        public string? SwiftCode { get; set; }
    }
}
