using Georgetown_Internationsl_Academy.API.DTO;

namespace Georgetown_Internationsl_Academy.API.Helpers
{
    public static class BambooEmployeeMapper
    {
        public static BambooHRDetailsDto ToDetailsDto(BambooHREmployeeXmlDto dto)
        {
            string Get(string key) => dto.Fields?.FirstOrDefault(f => f.Id.Equals(key, StringComparison.OrdinalIgnoreCase))?.Value;

            DateTime? ParseDate(string val) => DateTime.TryParse(val, out var dt) ? dt : null;

            return new BambooHRDetailsDto
            {
                Id = dto.Id,
                DisplayName = Get("displayName"),
                FirstName = Get("firstName"),
                LastName = Get("LastName"),
                Gender = Get("gender"),
                DateOfBirth = ParseDate(Get("dateofBirth")),
                Address1 = Get("address1"),
                Address2 = Get("address2"),
                MiddleName = Get("middleName"),
                WorkPhone = Get("workPhone"),
                MobilePhone = Get("mobilePhone"),
                City = Get("city"),
                State = Get("state"),
                ZipCode = Get("zipcode"),
                CommissionDate = ParseDate(Get("commissionDate")),
                Supervisor = Get("supervisor"),
                Status = Get("status"),
                TerminationDate = ParseDate(Get("terminationDate")),
                Department = Get("department"),
                JobTitle = Get("jobTitle"),
                WorkEmail = Get("workEmail"),
                HireDate = ParseDate(Get("hiredate")),
                EmployeeNumber = Get("employeenumber")

        };
        }
    }
}
