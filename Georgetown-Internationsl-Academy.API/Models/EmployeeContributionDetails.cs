namespace Georgetown_Internationsl_Academy.API.Models
{
    public class EmployeeContributionDetails
    {
        public string Surname { get; set; }
        public string EmployeeName { get; set; }
        public string NISNumber { get; set; }
        public string AgeGroup { get; set; }
        public decimal ActualEarnings { get; set; }
        public decimal InsurableEarnings { get; set; }
        public decimal EmployerContribution { get; set; }
        public decimal EmployeeContribution { get; set; }

    }
}
