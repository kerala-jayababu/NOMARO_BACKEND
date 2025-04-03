namespace Georgetown_Internationsl_Academy.API.DTO
{
    public class EmployeeDetailsDto
    {
        public int IdEmployee { get; set; }
        public string EmployeeCode { get; set; }
        public string FullName { get; set; }
        public string Designation { get; set; }
        public string Department { get; set; }
        public int IdBudgetCode { get; set; }
        public int ChildrenCount { get; set; }
        public int IdDepartment { get; set; }
        public int IdDesignation { get; set; }
        public string BudgetCodeName { get; set; }
        public string? EmployeePhotoFilePath { get; set; }
        public byte[]? AttachmentBlob { get; set; }


    }
}
