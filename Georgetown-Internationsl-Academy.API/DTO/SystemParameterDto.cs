namespace Georgetown_Internationsl_Academy.API.DTO
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
    }
}
