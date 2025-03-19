namespace Georgetown_Internationsl_Academy.API.DTO
{
    public class UploadSalaryGenerationDetailsDto
    {

        public int IdSalaryMonth { get; set; }

        public List<EmployeeSalaryJsonData> EmployeeSalaryJsonData { get; set; }


    }

    public class EmployeeSalaryJsonData
    {

        public string EmployeeCode { get; set; }

        public int IdSalaryHead { get; set; }
        public decimal SalaryAmount { get; set; }
    }
}
