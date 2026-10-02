using Microsoft.AspNetCore.Http;

namespace Nomaro.API.DTO
{
    /// <summary>A parameter shown on the System Parameters screen (ShowInUIToConfigure = true). ParameterBinaryValue is never read for the list.</summary>
    public class SystemParameterConfigDto
    {
        public int IdSystemParameter { get; set; }
        public string? ParameterName { get; set; }
        public string? ParameterDescription { get; set; }
        public string? ParameterValue { get; set; }
        public string? DataType { get; set; }
        public string? ValidValues { get; set; }
        public bool ParameterValueEditable { get; set; }
        public bool ParameterBinaryValueEditable { get; set; }
    }

    /// <summary>The stored image of a parameter, for the View popup.</summary>
    public class SystemParameterImageDto
    {
        public int IdSystemParameter { get; set; }
        public string? ParameterName { get; set; }
        public string ContentType { get; set; } = "image/png";
        public string ImageBase64 { get; set; } = string.Empty;
        public int ImageSizeBytes { get; set; }
    }

    /// <summary>
    /// Update from the System Parameters screen (multipart/form-data).
    /// ParameterValue is saved only when the parameter's value is editable;
    /// ImageFile is saved only when its image is editable.
    /// </summary>
    public class SystemParameterValueUpdateDto
    {
        public int IdSystemParameter { get; set; }
        public string? ParameterValue { get; set; }
        public IFormFile? ImageFile { get; set; }
    }
}
