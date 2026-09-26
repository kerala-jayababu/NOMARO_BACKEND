using System.ComponentModel.DataAnnotations.Schema;

namespace Nomaro.API.DTO
{
    public class SalaryUploadResponseDto
    {
        public int IdEmployee { get; set; }
        public string UploadStatus { get; set; }

        [NotMapped]
        public string? EmployeeCode { get; set; }
    }
}

