namespace Georgetown_Internationsl_Academy.API.DTO
{
    public class SalaryGenerationDto
    {
        public int IdEmployee { get; set; }
        public string EmployeeCode { get; set; }
        public string EmployeeName { get; set; }
        public int IdDesignation {  get; set; }
        public string DepartmentName { get; set; }
        public int IdDepartment {  get; set; }
        public string DesignationName { get; set; }
        public DateTime JoiningDate { get; set; }
        public DateTime? LastWorkingDay {  get; set; }
        public int? IdEmployeeSalary { get; set; }
        public string Gender { get; set; }
        public string EmailID { get; set; }
        public string PhoneNumber1 { get; set; }
        public string PhoneNumber2 { get; set; }
        public string CurrentStatus { get; set; }
        public decimal TotalEarnings { get; set; }
        public decimal TotalDeductions { get; set; }
        public decimal NetSalary { get; set; }
        public string ApprovalStatus { get; set; }
    }
}
