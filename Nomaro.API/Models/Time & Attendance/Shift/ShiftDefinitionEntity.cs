using System.ComponentModel.DataAnnotations;

namespace Nomaro.API.Models.Shift
{
    public class ShiftDefinitionEntity
    {
        [Key]
        public int IdShift { get; set; }

        [Required, MaxLength(50)]
        public string ShiftName { get; set; } = string.Empty;
        public bool IsRegularShiftJustTimeChange { get; set; }
        public int? IdOffice { get; set; }

    }
}

