using System.ComponentModel.DataAnnotations;

namespace Georgetown_Internationsl_Academy.API.Models
{
    public class SystemParameter
    {
        [Key]
        public int IdSystemParameter { get; set; }
        public string? ParameterName { get; set; } = string.Empty; 
        public string? ParameterDescription { get; set; } = string.Empty;
        public string? ParameterValue { get; set; } = string.Empty;
        public byte[]? ParameterBinaryValue { get; set; } 
        public string? DataType { get; set; } = string.Empty;
        public string? ValidValues { get; set; } = string.Empty;
    }
}
