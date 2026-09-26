using System.ComponentModel.DataAnnotations;

namespace Nomaro.API.DTO
{
    public class OfficeTypeDto
    {
        [Key]
        public int IdOfficeType { get; set; }
        public string OfficeTypeCode { get; set; }
        public string OfficeTypeName { get; set; }
        public int HierarchyLevel { get; set; }
        public bool IsActive { get; set; }
    }
}
