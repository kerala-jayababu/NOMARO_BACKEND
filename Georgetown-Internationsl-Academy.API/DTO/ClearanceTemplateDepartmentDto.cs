using System.ComponentModel.DataAnnotations;

namespace Georgetown_Internationsl_Academy.API.DTO
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