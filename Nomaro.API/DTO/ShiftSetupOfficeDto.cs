namespace Nomaro.API.DTO
{
    public class ShiftSetupOfficeDto
    {
        public int IdOffice { get; set; }
        public string OfficeCode { get; set; } = string.Empty;
        public string OfficeName { get; set; } = string.Empty;
        public int? IdParentOffice { get; set; }
        public int? Level { get; set; }
        public int? ShiftManager { get; set; }
        public string? ShiftManagerName { get; set; }
    }
}