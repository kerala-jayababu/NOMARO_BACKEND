using System.ComponentModel.DataAnnotations;

namespace Nomaro.API.Models
{
    public class ReportConditions
    {
        [Key]
        public int IdReportCondition { get; set; }
        public int IdReport { get; set; }
        public string? ConditionName { get; set; }
        public string? SPParameterName { get; set; }
        public string? ControlType { get; set; }
        public byte[]? DataType { get; set; }
        public char MandatoryFlag { get; set; }
        public string? TableName { get; set; }
        public string? ValueColumn { get; set; }
        public string? DisplayColumn { get; set; }
        public string? ValidValues { get; set; }
        public string? DefaultValue { get; set; }
        public string? WhereCondition { get; set; }
        public int OrderNumber { get; set; }
        public string? DefaultTime { get; set; }
    }
}

