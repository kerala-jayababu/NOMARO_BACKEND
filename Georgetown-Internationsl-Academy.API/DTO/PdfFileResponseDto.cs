namespace Georgetown_Internationsl_Academy.API.DTO
{
    public class PdfFileResponseDto
    {
        public string FileName { get; set; } = string.Empty;
        public string ContentType { get; set; } = "application/pdf";
        public string FileBytes { get; set; } = string.Empty;
    }
}
