using System.ComponentModel.DataAnnotations;

namespace Georgetown_Internationsl_Academy.API.Models
{
    public class DesignationEntity
    {
        [Key]
        public int IdDesignation { get; set; }
        public string DesignationCode { get; set; }
        public string DesignationName { get; set; }
        public bool IsOvertimeAllowanceAllowed { get; set; }
    }

}
