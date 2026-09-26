using System.ComponentModel.DataAnnotations;

namespace Nomaro.API.Models
{
    public class EmployeeTypes
    {
        [Key]
        public string EmployeeTypeName { get; set; }

    }
}

