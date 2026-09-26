namespace Nomaro.API.DTO
{
    public class ReportColumnsDto
    {

       
        public int IdReportCondition { get; set; }
        public int IdReport { get; set; }
        public string? ColumnName { get; set; }
        public string? DataType { get; set; }
        public string? Alignment { get; set; }
        public int WidthInPixels { get; set; }
        public bool TotalRequired { get; set; }
    }
}

