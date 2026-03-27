using System.ComponentModel.DataAnnotations;

namespace Georgetown_Internationsl_Academy.API.Models
{
    public class UserLastActivity
    {
        [Key]
        public int Id { get; set; }

        [MaxLength(200)]
        public string? Email { get; set; }

        public DateTime? LastAccessed { get; set; }

        [MaxLength(200)]
        public string? Endpoint { get; set; }

        public DateTime UpdatedOn { get; set; } = DateTime.Now;
        public string? Method { get; set; }
    }
}
