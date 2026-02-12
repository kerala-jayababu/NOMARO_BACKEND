using System.ComponentModel.DataAnnotations;

namespace Georgetown_Internationsl_Academy.API.DTO
{
    public class WorkMonthsInYearDto
    {
        [Key]
        public int IdOrder { get; set; }
        public int IdSalaryMonth { get; set; }
        public int IdWorkYEar { get; set; }
        public string MonthName { get; set; }
    }
}
