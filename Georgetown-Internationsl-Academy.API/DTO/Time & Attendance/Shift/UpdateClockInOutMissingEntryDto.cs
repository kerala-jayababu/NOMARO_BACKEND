using System.Text.Json;
using System.Text.Json.Serialization;

namespace Georgetown_Internationsl_Academy.API.DTO.Time___Attendance.Shift
{
    public class UpdateClockInOutMissingEntryDto
    {
        public int IdClockDetail { get; set; }
        public int IdEmployee { get; set; }
        public string ClockType { get; set; } = string.Empty;
        [JsonConverter(typeof(CustomDateTimeConverter))]
        public DateTime Time { get; set; }
        public string? Reason { get; set; }
    }
    public class CustomDateTimeConverter : JsonConverter<DateTime>
    {
        private readonly string _format = "yyyy-MM-dd HH:mm:ss";

        public override DateTime Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            var dateString = reader.GetString();
            return DateTime.ParseExact(dateString, _format, null);
        }

        public override void Write(Utf8JsonWriter writer, DateTime value, JsonSerializerOptions options)
        {
            writer.WriteStringValue(value.ToString(_format));
        }
    }
}
