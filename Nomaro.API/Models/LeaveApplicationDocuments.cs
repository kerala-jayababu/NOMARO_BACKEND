
using System;
using System.ComponentModel.DataAnnotations;
namespace Nomaro.API.DTO
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

