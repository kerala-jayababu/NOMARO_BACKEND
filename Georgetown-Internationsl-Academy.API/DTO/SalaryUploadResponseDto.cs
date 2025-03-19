using System.ComponentModel.DataAnnotations.Schema;

namespace Georgetown_Internationsl_Academy.API.DTO
{
    public class SalaryUploadResponseDto
    {
        public int IdEmployee { get; set; }
        public string UploadStatus { get; set; }

        [NotMapped]
        public string? EmployeeCode { get; set; }
    }
}
