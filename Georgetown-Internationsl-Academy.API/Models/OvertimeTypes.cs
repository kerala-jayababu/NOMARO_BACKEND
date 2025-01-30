using System.ComponentModel.DataAnnotations;

namespace Georgetown_Internationsl_Academy.API.Models
{
    public class OvertimeTypes
    {
        [Key]
        public int IdOvertimeType { get; set; }
         public string OvertimeTypeName {  get; set; }

    }
}
