using System.Text.Json;

namespace Nomaro.API.DTO
{
    public class StoredProcedureDto
    {
        public string? StoredProcedureName { get; set; }
        public Dictionary<string, JsonElement> Parameters { get; set; }

    }
}

