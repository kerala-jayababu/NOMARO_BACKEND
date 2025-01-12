namespace Georgetown_Internationsl_Academy.API.DTO
{
    public class DesignationDto
    {
        public int? IdDesignation { get; set; }
        public string DesignationCode { get; set; }
        public string DesignationName { get; set; }
        public bool IsOvertimeAllowanceAllowed { get; set; }
    }
}
