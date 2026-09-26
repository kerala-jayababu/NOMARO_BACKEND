using System.ComponentModel.DataAnnotations;

namespace Nomaro.API.DTO
{
    public class ClearanceTemplateDepartmentDto
    {
        [Key]
        public int IdTemplateDept { get; set; }
        public int IdClearanceTemplate { get; set; }
        public int IdDepartment { get; set; }
        public string CheckListItem { get; set; }
        public bool IsMandatory { get; set; }
        public string? DepartmentName { get; set; }
        public List<DepartmentEmployees> DeptEmployees { get; set; }
       
    }
}
