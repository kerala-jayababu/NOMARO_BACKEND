using System.ComponentModel.DataAnnotations;

namespace Georgetown_Internationsl_Academy.API.Models
{
    public class DepartmentEntity
    {
        [Key]
        public int IdDepartment { get; set; }
        public string DepartmentCode { get; set; }
        public string DepartmentName { get; set; }
    }

}
