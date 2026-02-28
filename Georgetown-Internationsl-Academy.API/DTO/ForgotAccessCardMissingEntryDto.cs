using System.ComponentModel.DataAnnotations;

namespace Georgetown_Internationsl_Academy.API.DTO
{
    public class ForgotAccessCardMissingEntryDto
    {
        [Key]
        public int IdClockInDetail { get;set; }
        public int IdEmployee { get; set; }
        public string EntryType { get; set; }
        public DateTime EntryDate { get;set;}

        public DateTime EntryTime { get; set; }
        public string? Reason { get; set; }
    }
}
