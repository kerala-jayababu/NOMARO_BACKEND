using System.ComponentModel.DataAnnotations;

namespace Georgetown_Internationsl_Academy.API.Models
{
    public class ReportColumns
    {

        [Key]
        public int IdReportCondition { get; set; }
        public int IdReport { get; set; }
        public string? ColumnName { get; set; }
        public string? DataType { get; set; }
        public string? Alignment { get; set; }
        public int WidthInPixels { get; set; }
        public bool TotalRequired { get; set; }
    }
}
