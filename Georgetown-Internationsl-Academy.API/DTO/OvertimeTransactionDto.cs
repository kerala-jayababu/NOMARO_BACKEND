using System;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.AspNetCore.Http;

namespace Georgetown_Internationsl_Academy.API.DTO
{
    public class OvertimeTransactionDto
    {
        public int IdOvertimeTransaction { get; set; }
        public int IdEmployee { get; set; }
        public int? IdOvertimeType { get; set; }  
        public DateTime StartDate { get; set; }
        public TimeSpan StartTime { get; set; }
        public DateTime EndDate { get; set; }
        public TimeSpan EndTime { get; set; }
        public decimal DurationInHours { get; set; }
        public string? ReasonForOvertime { get; set; } = string.Empty;
        public string? Attachment { get; set; }
        public string? AttachmentDescription { get; set; }
        public IFormFile? File { get; set; }
        public string? ApprovalStatus { get; set; }
        public string? DayType { get; set; }

        public string? CreatedBy { get; set; }
        public string? SalaryMonthText { get; set; }
        public decimal? SalaryAccountedAmount { get; set; }
        public string? CreatedOn { get; set; }
        [NotMapped]
        public string? Apptype { get; set; }


        [NotMapped]
        public byte[]? AttachmentBlob { get; set; }
        // Employee Related Details (Directly Mapped from Database)
        [NotMapped]
        public string EmployeeCode { get; set; } = string.Empty;
        [NotMapped]
        public string EmployeeName { get; set; } = string.Empty;
        [NotMapped]
        public string Department { get; set; } = string.Empty;
        [NotMapped]
        public string Designation { get; set; } = string.Empty;
        [NotMapped]
        public int IdDepartment { get; set; }
        [NotMapped]
        public int IdDesignation { get; set; }
        [NotMapped]
        public string? DurationText { get; set; }

        [NotMapped]
        public decimal? OTAmount { get; set; }
        [NotMapped]
        public List<EmployeeOvertimeConfigDto> OvertimeConfigs { get; set; } = new();
    }
}
