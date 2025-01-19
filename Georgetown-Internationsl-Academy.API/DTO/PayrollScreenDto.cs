namespace Georgetown_Internationsl_Academy.API.DTO
{
    public class PayrollScreenDto
    {
        public int? IdPayrollScreen { get; set; } 
        public string ScreenName { get; set; }
        public string? ValidPermissions { get; set; }
        public int? IdParentPayrollScreen { get; set; }
    }
}
