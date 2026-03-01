using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;

namespace Georgetown_Internationsl_Academy.API.DTO.Shift
{
    public class ShiftDto
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int IdShift { get; set; }
        public string ShiftName { get; set; } = string.Empty;
        public bool IsRegularShiftJustTimeChange { get; set; }

    }
}
