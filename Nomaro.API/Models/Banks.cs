using System.ComponentModel.DataAnnotations;

namespace Nomaro.API.Models
{
    public class Banks
    {
        [Key]
        public int IdBank { get; set; }
        public string BankName { get; set; }
        public string? SwiftCode { get; set; }

    }
}

