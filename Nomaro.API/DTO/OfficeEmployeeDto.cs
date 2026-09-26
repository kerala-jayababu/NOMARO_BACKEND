namespace Nomaro.API.DTO
{
    public class OfficeEmployeeDto
    {
        public int IdEmployeeOfficePosting { get; set; }
        public int IdEmployee { get; set; }
        public string? EmployeeCode { get; set; }
        public string? EmployeeName { get; set; }
        public string? DepartmentName { get; set; }
        public string? DesignationName { get; set; }
        public string? EmailID { get; set; }
        public string? PhoneNumber1 { get; set; }
        public string? CurrentStatus { get; set; }
        public DateTime PostingFromDate { get; set; }
        public DateTime? PostingToDate { get; set; }
        public string? PostingType { get; set; }
        public bool IsCurrentPosting { get; set; }
    }
}
