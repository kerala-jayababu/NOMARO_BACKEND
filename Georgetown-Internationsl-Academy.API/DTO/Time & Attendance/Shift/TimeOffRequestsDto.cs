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

}
