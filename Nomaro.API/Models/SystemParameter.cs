using System.ComponentModel.DataAnnotations;

namespace Nomaro.API.Models
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
        public bool? ShowInUIToConfigure { get; set; }
        public bool? ParameterValueEditable { get; set; }
        public bool? ParameterBinaryValueEditable { get; set; }
    }
}

