namespace Georgetown_Internationsl_Academy.API.DTO
{
    public class LeavePassageAmountDto
    {
        public int IdEmployee { get; set; }
        public string EmployeeName { get; set; }
        public string EmployeeCode { get; set; }
        public int IdDesignation { get; set; }
        public string DesignationName { get; set; }
        public int IdDepartment { get; set; }
        public string DepartmentName { get; set; }
        public DateTime JoiningDate { get; set; }
        public string Gender { get; set; }
        public string EmailID { get; set; }
        public string PhoneNumber1 { get; set; }
        public string PhoneNumber2 { get; set; }

        // Leave Passage Amount fields
        public int? IdLeavePassageAmount { get; set; }
        public int? IdFinancialYear { get; set; }
        public DateTime? DateFrom { get; set; }
        public DateTime? DateTo { get; set; }
        public decimal? LeavePassageAmount { get; set; }

        // Financial Year details
        public string FinancialYearName { get; set; }
        public DateTime? FinancialYearFrom { get; set; }
        public DateTime? FinancialYearTo { get; set; }
    }


}
