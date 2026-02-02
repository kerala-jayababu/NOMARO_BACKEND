using System.ComponentModel.DataAnnotations;

namespace Georgetown_Internationsl_Academy.API.DTO
{
    public class EmployeeTypeDto
    {        
        [Key]
        public string EmployeeTypeName { get; set; }       
    }
}
