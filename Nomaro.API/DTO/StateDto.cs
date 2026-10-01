namespace Nomaro.API.DTO
{
    public class StateDto
    {
        public int IdState { get; set; }
        public string StateCode { get; set; }
        public string StateName { get; set; }
        public bool HasPT { get; set; }
        public bool HasLWF { get; set; }
        public bool IsActive { get; set; }
    }
}
