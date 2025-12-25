using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Georgetown_Internationsl_Academy.API.Models
{
    public class AssetAssignments
    {
        [Key]
        public int IdAssetAssignment { get; set; }
        public int IdAsset { get; set; }
        public int IdEmployee { get; set; }
        public DateTime AssignedDate { get; set; }
        public DateTime? AssignedTillDate { get; set; }
        public string? Remarks { get; set; }
        public int AssignedBy { get; set; }
        public DateTime AssignedDateTime { get; set; }
        [ForeignKey("IdAsset")]
        public virtual Assets Asset { get; set; }

    }
}