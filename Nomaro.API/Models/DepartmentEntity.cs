using System.ComponentModel.DataAnnotations;

namespace Nomaro.API.Models
{
    public class DepartmentEntity
    {
        [Key]
        public int IdDepartment { get; set; }
        public string DepartmentCode { get; set; }
        public string DepartmentName { get; set; }
    }

}

