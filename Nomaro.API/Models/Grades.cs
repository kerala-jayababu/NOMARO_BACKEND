using System.ComponentModel.DataAnnotations;

namespace Nomaro.API.Models
{
    // Employee grade / level (e.g. G1 - Entry Level ... G11 - Executive Leadership)
    public class Grades
    {
        [Key]
        public int IdGrade { get; set; }
        public string GradeCode { get; set; }
        public string GradeName { get; set; }
        public int GradeLevel { get; set; }
        public string? Description { get; set; }
        public bool IsActive { get; set; }
        public int? IdCreatedBy { get; set; }
        public DateTime CreatedDateTime { get; set; }
        public int? IdModifiedBy { get; set; }
        public DateTime? ModifiedDateTime { get; set; }
    }
}
