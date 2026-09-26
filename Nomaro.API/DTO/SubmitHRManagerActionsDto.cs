using System.ComponentModel.DataAnnotations;

namespace Nomaro.API.DTO
{
    public class SubmitHRManagerActionsDto
    {
        [Required]
        public int IdExitCase { get; set; }

        [Required]
        public int IdEmployee { get; set; }

        public DateTime? ExitInterviewDate { get; set; }

        [Required]
        [StringLength(2000)]
        public string ExitInterviewDetails { get; set; }
    }
    public class HRManagerActionResponseDto
    {
        public bool Success { get; set; }
        public int IdExitCase { get; set; }
        public string Message { get; set; }
        public List<string> Errors { get; set; } = new();
    }
}

