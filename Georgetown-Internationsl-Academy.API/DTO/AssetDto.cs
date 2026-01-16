using System.ComponentModel.DataAnnotations;

namespace Georgetown_Internationsl_Academy.API.DTO
{
    public class ClearanceTemplateDto
    {
        [Key]
        public int IdClearanceTemplate { get; set; }
        public string TemplateName { get; set; }
        public string? Description { get; set; }
        public bool IsActive { get; set; }
    }
}