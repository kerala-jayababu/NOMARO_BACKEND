namespace Georgetown_Internationsl_Academy.API.DTO
{
    public class EmployeePermissionDto
    {
        public int? IdEmployeePermission { get; set; } 
        public int IdEmployee { get; set; }
        public int IdPayrollScreen { get; set; }
        public string Permission { get; set; }
        public string? ScreenName { get; set; }
    }
}
