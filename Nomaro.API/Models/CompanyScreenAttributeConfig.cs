using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Nomaro.API.Models
{
    [Table("CompanyScreenAttributeConfig", Schema = "dbo")]
    public class CompanyScreenAttributeConfig
    {
        [Key]
        public int IdCompanyScreenAttributeConfig { get; set; }

        [Required, MaxLength(50)]
        public string CompanyCode { get; set; } = string.Empty;

        [Required, MaxLength(50)]
        public string ScreenCode { get; set; } = string.Empty;

        [Required, MaxLength(100)]
        [Column("AttributeID")]
        public string AttributeId { get; set; } = string.Empty;

        [MaxLength(100)]
        public string? DisplayLabel { get; set; }

        public bool? FieldVisibility { get; set; }
        public int? MaxLength { get; set; }

        [MaxLength(50)]
        public string? DataType { get; set; }
    }
}
