using System;
using System.ComponentModel.DataAnnotations;

namespace Georgetown_Internationsl_Academy.API.Models
{
    public class EmployeeServiceChanges
    {
        [Key]
        public int IdEmployeeServiceChange { get; set; }
        public int IdEmployee { get; set; }
        public string? ChangeType { get; set; }
        public string? ChangeDescription { get; set; }
        public string? FromValue { get; set; }
        public string? ToValue { get; set; }
        public int FromValueID { get; set; }
        public int ToValueID { get; set; }
        public DateTime ChangeValidFrom { get; set; }
        public int ChangedBy { get; set; }
        public string? Remarks { get; set; }
        public string? ApprovalStatus { get; set; }
        public int? ApprovedBy { get; set; }
        public DateTime? ApprovedDate { get; set; }
        public int CreatedBy { get; set; }
        public DateTime CreatedAt { get; set; }
        public int? UpdatedBy { get; set; }
        public DateTime? UpdatedAt { get; set; }
    }
}