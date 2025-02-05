namespace Georgetown_Internationsl_Academy.API.DTO
{
    public class EmployeeHierarchyDto
    {
        public int IdEmployee { get; set; }
        public string EmployeeCode { get; set; }
        public string EmployeeName { get; set; }
        public int IdDesignation { get; set; }
        public string DesignationName { get; set; }
        public int IdDepartment { get; set; }
        public string DepartmentName { get; set; }
        public int? ReportingTo { get; set; }
        public string? IdReportingToName { get; set; }
    }
}
