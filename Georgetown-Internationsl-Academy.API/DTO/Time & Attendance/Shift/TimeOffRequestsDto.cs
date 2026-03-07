using System.Xml.Serialization;

namespace Georgetown_Internationsl_Academy.API.DTO.Time___Attendance.Shift
{
    [XmlRoot("requests")]
    public class TimeOffRequestsDto
    {
        [XmlElement("request")]
        public List<TimeOffRequestDto> Requests { get; set; }
    }

    public class TimeOffRequestDto
    {
        [XmlAttribute("id")]
        public int Id { get; set; }

        [XmlElement("employee")]
        public EmployeeDto Employee { get; set; }

        [XmlElement("status")]
        public StatusDto Status { get; set; }
    

        [XmlElement("start")]
        public DateTime Start { get; set; }

        [XmlElement("end")]
        public DateTime End { get; set; }

        [XmlElement("created")]
        public DateTime Created { get; set; }

        [XmlElement("type")]
        public TypeDto Type { get; set; }

        [XmlElement("amount")]
        public AmountDto Amount { get; set; }

        [XmlArray("dates")]
        [XmlArrayItem("date")]
        public List<LeaveDateDto> Dates { get; set; }

        [XmlElement("notes", IsNullable = true)]
        public NotesDto Notes { get; set; }
    }
    public class NotesDto
    {
        [XmlElement("note")]
        public List<NoteDto> Notes { get; set; } // List of notes, if there are any

    }

    // Define a class for the note itself
    public class NoteDto
    {
        [XmlAttribute("from")]
        public string From { get; set; }

        [XmlText]
        public string Value { get; set; }
    }
    public class EmployeeDto
    {
        [XmlAttribute("id")]
        public int Id { get; set; }

        [XmlText]
        public string Name { get; set; }
    }

    public class StatusDto
    {
        [XmlAttribute("lastChanged")]
        public DateTime LastChanged { get; set; }
        [XmlAttribute("lastChangedByUserId")]
        public string LastChangedByUserId { get; set; }

        [XmlText]
        public string Value { get; set; }
    }

    public class TypeDto
    {
        [XmlAttribute("id")]
        public int Id { get; set; }

        [XmlText]
        public string Value { get; set; }
    }

    public class AmountDto
    {
        [XmlAttribute("unit")]
        public string Unit { get; set; }

        [XmlText]
        public decimal Value { get; set; }
    }

    public class LeaveDateDto
    {
        [XmlAttribute("ymd")]
        public DateTime Ymd { get; set; }

        [XmlAttribute("amount")]
        public decimal Amount { get; set; }
    }

    public class LeaveTemplateQueryDto
    {
        public int IdEmployee { get; set; }
        public int IdEmployeeLeaveConfigDetails { get; set; }
        public int IdLeaveType { get; set; }
        public string LeaveTypeName { get; set; }
        public int IdLeaveTemplateDetails { get; set; }
        public DateTime EffectiveFrom { get; set; }
        public DateTime EffectiveTo { get; set; }
        public string LeaveTemplateName { get; set; }
        public int IdYear { get; set; }
    }
    [XmlRoot("users")]
    public class BambooUsersDto
    {
        [XmlElement("user")]
        public List<BambooUserDto> Users { get; set; }
    }
    public class BambooUserDto
    {
        [XmlAttribute("id")]
        public string Id { get; set; }

        [XmlAttribute("employeeId")]
        public string EmployeeId { get; set; }

        [XmlElement("firstName")]
        public string FirstName { get; set; }

        [XmlElement("lastName")]
        public string LastName { get; set; }

        [XmlElement("email")]
        public string Email { get; set; }

        [XmlElement("lastLogin")]
        public DateTime LastLogin { get; set; }

        [XmlElement("status")]
        public string Status { get; set; }
    }


}
