using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Nomaro.API.Models
{
    public class ClearanceTemplateDepartments
    {
        [Key]
        public int IdTemplateDept { get; set; }
        public int IdClearanceTemplate { get; set; }
        public int IdDepartment { get; set; }
        public string CheckListItem { get; set; }
        public bool IsMandatory { get; set; }
    }

}
