using System.Text.Json;

namespace Georgetown_Internationsl_Academy.API.DTO
{
    public class StoredProcedureDto
    {
        public string? StoredProcedureName { get; set; }
        public Dictionary<string, JsonElement> Parameters { get; set; }

    }
}
