namespace Nomaro.API.DTO
{
    public class LeaveApplicationPostDto
    {
        public int IdLeaveApplication { get; set; }  // 0=insert

        public int IdEmployee { get; set; }
        public int IdLeaveType { get; set; }
        public string LeaveTypeName { get; set; }
        public DateTime FromDate { get; set; }
        public DateTime ToDate { get; set; }
        public decimal TotalNoOfDays { get; set; }
        public bool IsHalfDay { get; set; }
        public char? HalfDayType { get; set; }   // FirstHalf / SecondHalf (or AM/PM)

        public string Reason { get; set; } = string.Empty;
        public List<IFormFile>? Documents { get; set; }
    }
}

