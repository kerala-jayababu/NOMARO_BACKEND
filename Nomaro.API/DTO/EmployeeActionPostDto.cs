namespace Nomaro.API.DTO
{
    public class EmployeeActionPostDto
    {
        public int IdEmployeeAction { get; set; }   // 0 = Insert, >0 = Update
        public int IdEmployee { get; set; }

        public string? ActionType { get; set; }     // RECOGNITION / DISCIPLINARY
        public string? ActionDescription { get; set; }
        public string? ActionSeverity { get; set; } // Low / Medium / High / Very High
        public string? Remarks { get; set; }

        public DateTime EffectiveFromDate { get; set; }
        public DateTime? EffectiveToDate { get; set; }

        public string? Status { get; set; }
    }
}

