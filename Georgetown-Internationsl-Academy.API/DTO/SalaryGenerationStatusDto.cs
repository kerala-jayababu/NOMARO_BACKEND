namespace Georgetown_Internationsl_Academy.API.DTO
{
    public class SalaryGenerationStatusDto
    {
        public int IdEmployee { get; set; }
        public string SalaryGenerationStatus { get; set; } = string.Empty;
        public string Remarks { get; set; } = string.Empty;
    }
}
