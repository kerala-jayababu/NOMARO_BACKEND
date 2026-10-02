namespace Nomaro.API.DTO
{
    public class SystemParameterDto
    {
        public int IdSystemParameter { get; set; }
        public string ParameterName { get; set; }
        public string? ParameterDescription { get; set; }
        public string? ParameterValue { get; set; }
        public byte[]?   ParameterBinaryValue { get; set; }
        public string? DataType { get; set; }
        public string? ValidValues { get; set; }
        public bool ShowInUIToConfigure { get; set; }
        public bool ParameterValueEditable { get; set; }
        public bool ParameterBinaryValueEditable { get; set; }
    }
}

