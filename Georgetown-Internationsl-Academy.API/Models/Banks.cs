using System.ComponentModel.DataAnnotations;

namespace Georgetown_Internationsl_Academy.API.Models
{
    public class Banks
    {
        [Key]
        public int IdBank { get; set; }
        public string BankName { get; set; }
        public string SwiftCode { get; set; }

    }
}
