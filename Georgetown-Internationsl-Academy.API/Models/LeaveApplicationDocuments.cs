
using System;
using System.ComponentModel.DataAnnotations;
namespace Georgetown_Internationsl_Academy.API.DTO
{
    public class LeaveApplicationDocuments
    {
        [Key]
        public int IdLeaveApplicationDocument { get; set; }
        public int IdLeaveApplication { get; set; }
        public string FileType { get; set; }
        public string FileName { get; set; }
        public string FilePath { get; set; }
        public DateTime UploadedAt { get; set; }
    }
}
