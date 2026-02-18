namespace Georgetown_Internationsl_Academy.API.DTO
{
    public class EmployeeLeaveConfigWithDetailsPostDto
    {
        public int IdEmployeeLeaveConfig { get; set; }
        public int IdEmployee { get; set; }
        public int IdLeaveTemplate { get; set; }
        public DateTime EffectiveFrom { get; set; }
        public DateTime? EffectiveTo { get; set; }
        public List<EmployeeLeaveConfigDetailsPostDto> Details { get; set; } = new();
    }

}
