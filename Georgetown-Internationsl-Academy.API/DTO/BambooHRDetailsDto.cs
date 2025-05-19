namespace Georgetown_Internationsl_Academy.API.DTO
{
    public class BambooHRDetailsDto
    {
        public string Id { get; set; }

        public string EmployeeNumber { get; set; }
        public string DisplayName { get; set; }
        public string FirstName { get; set; }
        public string LastName { get; set; }
        public string Gender { get; set; }
        public DateTime? DateOfBirth { get; set; }
        public string Address1 { get; set; }
        public string Address2 { get; set; }
        public string MiddleName { get; set; }
        public string WorkPhone { get; set; }
        public string MobilePhone { get; set; }
        public string City { get; set; }
        public string State { get; set; }
        public string ZipCode { get; set; }
        public DateTime? CommissionDate { get; set; }
        public string Supervisor { get; set; }
        public string Status { get; set; }
        public DateTime? TerminationDate { get; set; }
        public string Department { get; set; }
        public string JobTitle { get; set; }
        public string WorkEmail { get; set; }
        public DateTime? HireDate { get; set; }
        public string? EmployeePhotoPath { get; set; }
        public string? customNIS { get; set; }
        public string? customTIN { get; set; }

    }
}
