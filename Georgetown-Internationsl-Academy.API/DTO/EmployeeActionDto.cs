using System;
using System.ComponentModel.DataAnnotations;

namespace Georgetown_Internationsl_Academy.API.DTO
{
    public class EmployeeActionDto
    {
        [Key]
        public int IdEmployeeAction { get; set; }
        public int IdEmployee { get; set; }
        public string? ActionType { get; set; }
        public string? ActionDescription { get; set; }
        public string? ActionSeverity { get; set; }
        public string? Status { get; set; }
    }
}