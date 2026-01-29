using System.ComponentModel.DataAnnotations;

namespace Georgetown_Internationsl_Academy.API.DTO
{
    public class EmployeeTypeDto
    {
        public class EmployeeTypes
        {
            [Key]
            public string EmployeeTypeName { get; set; }

        }
    }
}
