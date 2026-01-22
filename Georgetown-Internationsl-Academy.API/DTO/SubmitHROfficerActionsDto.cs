using System.ComponentModel.DataAnnotations;

namespace Georgetown_Internationsl_Academy.API.DTO
{
    public class SubmitHROfficerActionsDto
    {
        [Required]
        public int IdExitCase { get; set; }

        [Required]
        public int IdEmployee { get; set; }

        public DateTime? ApprovedLWD { get; set; }

        public DateTime? ExitInterviewDate { get; set; }

        public string? ContactAfterExit { get; set; }

        [Required]
        public int IdClearanceTemplate { get; set; }

        [Required]
        public List<ClearanceAssignmentDto> ClearanceAssignments { get; set; } = new();
    }

    public class ClearanceAssignmentDto
    {
        public int IdExitCaseClearanceAssignment { get; set; } // 0 = Insert
        public int IdDepartment { get; set; }
        public int IdTemplateDept { get; set; }
        public int IdAssigneeUser { get; set; }
    }
    public class HROfficerActionResponseDto
    {
        public bool Success { get; set; }
        public int IdExitCase { get; set; }
        public string? Message { get; set; }
        public List<string>? Errors { get; set; }
    }
}
