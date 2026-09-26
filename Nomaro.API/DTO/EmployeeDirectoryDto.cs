using System.Xml.Serialization;

namespace Nomaro.API.DTO
{
    [XmlRoot("directory")]
    public class EmployeeDirectoryDto
    {
        [XmlArray("employees")]
        [XmlArrayItem("employee")]
        public List<EmployeeRawDto> Employees { get; set; }
    }
    public class EmployeeRawDto
    {
        [XmlAttribute("id")]
        public string Id { get; set; }

        [XmlElement("field")]
        public List<EmployeeField> Fields { get; set; }

        [XmlIgnore]
        public string EmployeePhotoPath { get; set; } // For mapping image URL
    }
    public class EmployeeField
    {
        [XmlAttribute("id")]
        public string Id { get; set; }

        [XmlText]
        public string Value { get; set; }
    }
}

