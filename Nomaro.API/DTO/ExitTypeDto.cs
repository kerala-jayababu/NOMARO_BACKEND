using System.ComponentModel.DataAnnotations;

namespace Nomaro.API.DTO
{
    public class ExitTypeDto
    {
        [Key]
        public int IdExitType { get; set; }
        public string TypeCode { get; set; }
        public string TypeName { get; set; }
        public bool IsActive { get; set; }
    }
}
