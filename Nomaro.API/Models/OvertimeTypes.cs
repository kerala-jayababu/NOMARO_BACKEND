using System.ComponentModel.DataAnnotations;

namespace Nomaro.API.Models
{
    public class OvertimeTypes
    {
        [Key]
        public int IdOvertimeType { get; set; }
         public string OvertimeTypeName {  get; set; }

    }
}

