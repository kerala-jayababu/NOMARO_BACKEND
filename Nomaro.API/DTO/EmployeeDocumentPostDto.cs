using System.ComponentModel.DataAnnotations;

namespace Nomaro.API.DTO
{
    public class EmployeeDocumentPostDto
    {
        [Key]
        public int IdEmployeeDocument { get; set; }  // 0 → Insert, >0 → Update
        public int IdEmployee { get; set; }
        public int IdDocumentType { get; set; }
        public string? Remarks { get; set; }
        public DateTime? DocumentValidTill { get; set; }

        // ✅ File upload
        public IFormFile? DocumentFile { get; set; }
    }
}

