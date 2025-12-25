using System;
using System.ComponentModel.DataAnnotations;

namespace Georgetown_Internationsl_Academy.API.DTO
{
    public class EmployeeServiceChangeDto
    {
        [Key]
        public int IdEmployeeServiceChange { get; set; }
        public int IdEmployee { get; set; }
        public string? ChangeType { get; set; }
        public string? ChangeDescription { get; set; }
        public string? FromValue { get; set; }
        public string? ToValue { get; set; }
        public DateTime ChangedDate { get; set; }
        public string? ApprovalStatus { get; set; }
    }
}