using System.Xml.Serialization;

namespace Nomaro.API.DTO
{
  
    [XmlRoot("employee")]
    public class BambooHREmployeeXmlDto
    {
        [XmlAttribute("id")]
        public string Id { get; set; }

        [XmlElement("field")]
        public List<BambooHRFieldDto> Fields { get; set; }
    }

    public class BambooHRFieldDto
    {
        [XmlAttribute("id")]
        public string Id { get; set; }

        [XmlText]
        public string Value { get; set; }
    }
}

