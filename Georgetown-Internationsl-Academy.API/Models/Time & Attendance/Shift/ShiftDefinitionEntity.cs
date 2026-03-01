using System.ComponentModel.DataAnnotations;

namespace Georgetown_Internationsl_Academy.API.Models.Shift
{
    public class ShiftDefinitionEntity
    {
        [Key]
        public int IdShift { get; set; }

        [Required, MaxLength(50)]
        public string ShiftName { get; set; } = string.Empty;
        public bool IsRegularShiftJustTimeChange { get; set; }

    }
}
