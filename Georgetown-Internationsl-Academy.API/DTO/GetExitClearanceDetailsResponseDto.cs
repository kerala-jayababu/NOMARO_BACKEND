namespace Georgetown_Internationsl_Academy.API.DTO
{
    public class GetExitClearanceDetailsResponseDto
    {
        public bool Success { get; set; }
        public string? Message { get; set; }
        public ExitClearanceCaseHeaderDto? ExitCase { get; set; }
        public List<ExitClearanceDepartmentBlockDto> Departments { get; set; } = new();
    }

    public class ExitClearanceCaseHeaderDto
    {
        public int IdExitCase { get; set; }
        public string? CaseNumber { get; set; }
        public int IdEmployee { get; set; }
        public string? EmployeeName { get; set; }
        public string? ExitStatus { get; set; }
        public string? PendingWith { get; set; }
        public DateTime InitiationDate { get; set; }
        public DateTime ProposedLWD { get; set; }
        public DateTime? ApprovedLWD { get; set; }
        public int? IdClearanceTemplate { get; set; }
        public DateTime? ClearanceInitiatedOn { get; set; }
    }

    public class DeptHeaderDto
    {
        public string? DeptClearanceStatus { get; set; }
        public string? DeptRemarks { get; set; }
        public int? ClearedBy { get; set; }
        public string? ClearedByName { get; set; }
        public DateTime? ClearedAt { get; set; }
    }

    public class ExitClearanceDepartmentBlockDto
    {
        public int IdDepartment { get; set; }
        public string? DepartmentName { get; set; }

        public int IdAssigneeUser { get; set; }
        public string? AssigneeName { get; set; }
        public string? DeptClearanceStatus { get; set; }
        public DateTime? AssignedAt { get; set; }

        public bool CanEditChecklist { get; set; }
        public bool CanEditDeptHeader { get; set; }

        public DeptHeaderDto Header { get; set; } = new();
        public List<ExitClearanceChecklistItemDto> Checklist { get; set; } = new();
    }

    public class ExitClearanceChecklistItemDto
    {
        public int IdExitCaseDepartmentClearanceLine { get; set; }
        public string? CheckListItem { get; set; }
        public string? DeptClearanceStatus { get; set; }
        public int? SortOrder { get; set; }
    }

}
