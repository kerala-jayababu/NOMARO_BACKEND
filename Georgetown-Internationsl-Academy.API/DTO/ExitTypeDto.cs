using System.ComponentModel.DataAnnotations;

namespace Georgetown_Internationsl_Academy.API.DTO
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