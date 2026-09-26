using System.ComponentModel.DataAnnotations;

namespace Nomaro.API.DTO
{
    public class EmployeeTypeDto
    {        
        [Key]
        public string EmployeeTypeName { get; set; }       
    }
}

