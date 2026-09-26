using System.ComponentModel.DataAnnotations;

namespace Nomaro.API.Models
{
    public class OfficeTypes
    {
        [Key]
        public int IdOfficeType { get; set; }
        public string OfficeTypeCode { get; set; }
        public string OfficeTypeName { get; set; }
        public int HierarchyLevel { get; set; }
        public bool IsActive { get; set; }
        public int? IdCreatedBy { get; set; }
        public DateTime CreatedDateTime { get; set; }
        public int? IdModifiedBy { get; set; }
        public DateTime? ModifiedDateTime { get; set; }
    }
}
