using System.ComponentModel.DataAnnotations;

namespace Nomaro.API.DTO
{
    public class ServiceChangeApprovalDto
    {

        [Key]
        public int IdEmployeeServiceChange { get; set; }
        public int IdEmployee { get; set; }
        public string? ChangeType { get; set; }
        public string? ChangeDescription { get; set; }
    }
}

