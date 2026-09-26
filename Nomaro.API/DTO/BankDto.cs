using System.ComponentModel.DataAnnotations;

namespace Nomaro.API.DTO
{
    public class BankDto
    {
        [Key]
        public int IdBank { get; set; }
        public string BankName { get; set; }
        public string? SwiftCode { get; set; }
    }
}

