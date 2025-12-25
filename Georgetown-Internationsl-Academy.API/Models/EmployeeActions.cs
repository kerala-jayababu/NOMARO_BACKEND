using System;
using System.ComponentModel.DataAnnotations;

namespace Georgetown_Internationsl_Academy.API.Models
{
    public class EmployeeActions
    {
        [Key]
        public int IdEmployeeAction { get; set; }
        public int IdEmployee { get; set; }
        public string? ActionType { get; set; }
        public string? ActionDescription { get; set; }
        public string? ActionSeverity { get; set; }
        public string? Remarks { get; set; }
        public DateTime EffectiveFromDate { get; set; }
        public DateTime? EffectiveToDate { get; set; }
        public string? Status { get; set; }
        public int CreatedBy { get; set; }
        public DateTime CreatedDate { get; set; }
        public int? ApprovedBy { get; set; }
        public DateTime? ApprovedDate { get; set; }
    }
}