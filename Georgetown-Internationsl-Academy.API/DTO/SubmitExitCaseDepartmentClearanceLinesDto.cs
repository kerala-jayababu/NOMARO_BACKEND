using System.ComponentModel.DataAnnotations;

namespace Georgetown_Internationsl_Academy.API.DTO
{
    public class SubmitExitCaseDepartmentClearanceLinesDto
    {
        [Required]
        public int IdExitCase { get; set; }

        [Required]
        public int IdDepartment { get; set; }

        public string? DeptRemarks { get; set; }

        // ⚠️ Your DB doesn't have DueAmount column.
        // If you add it, store on header row.
        public decimal? DueAmount { get; set; }

        // Optional: "PENDING", "CLEARED", "NOT_APPLICABLE"
        public string? DeptClearanceStatus { get; set; }

        public bool? MarkDepartmentCleared { get; set; }

        public List<ClearanceLineUpdateDto>? ClearanceLineUpdates { get; set; }
    }

    public class ClearanceLineUpdateDto
    {
        [Required]
        public int IdExitCaseDepartmentClearanceLine { get; set; }

        [Required]
        public bool IsCompleted { get; set; }
    }

    public class SubmitExitCaseDepartmentClearanceLinesResponseDto
    {
        public bool Success { get; set; }
        public string? Message { get; set; }

        public int IdExitCase { get; set; }
        public int IdDepartment { get; set; }

        public string? DepartmentStatus { get; set; }
        public string? DeptRemarks { get; set; }
        public decimal? DueAmount { get; set; }
        public int? ClearedBy { get; set; }
        public DateTime? ClearedAt { get; set; }

        public string? CaseExitStatus { get; set; }
        public string? CasePendingWith { get; set; }

        public List<UpdatedChecklistItemDto> Checklist { get; set; } = new();
        public List<string> Errors { get; set; } = new();
    }

    public class UpdatedChecklistItemDto
    {
        public int IdExitCaseDepartmentClearanceLine { get; set; }
        public string? CheckListItem { get; set; }
        public bool IsCompleted { get; set; }
        public string? LineStatus { get; set; }
    }

}
