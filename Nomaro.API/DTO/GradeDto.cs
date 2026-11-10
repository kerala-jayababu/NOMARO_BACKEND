namespace Nomaro.API.DTO
{
    public class GradeDto
    {
        public int IdGrade { get; set; }
        public string GradeCode { get; set; }
        public string GradeName { get; set; }
        public int GradeLevel { get; set; }
        public string? Description { get; set; }
        public bool IsActive { get; set; }
        public int EmployeeCount { get; set; }
    }
}
