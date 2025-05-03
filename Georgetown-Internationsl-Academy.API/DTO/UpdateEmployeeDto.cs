using System.ComponentModel.DataAnnotations.Schema;

namespace Georgetown_Internationsl_Academy.API.DTO
{
    public class UpdateEmployeeDto
    {
        public int EmployeeId { get; set; }
        public int BudgetCodeId { get; set; }
        public int ChildCount { get; set; }
        public IFormFile? File { get; set; }
        public string? ChildCountDocumentFilePath { get; set; }

        [NotMapped]
        public byte[]? AttachmentBlob { get; set; }
    }
}
