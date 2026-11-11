namespace Nomaro.API.DTO
{
    /// <summary>Defines display behavior for one attribute on a company-specific screen.</summary>
    public sealed record ScreenAttributeConfigDto
    {
        /// <summary>The stable identifier used to match the configured field in the UI.</summary>
        public string AttributeId { get; init; } = string.Empty;

        /// <summary>The label override, or null to retain the UI default.</summary>
        public string? DisplayLabel { get; init; }

        /// <summary>Whether the field is visible, or null to retain the UI default.</summary>
        public bool? FieldVisibility { get; init; }

        /// <summary>The configured maximum input length, when applicable.</summary>
        public int? MaxLength { get; init; }

        /// <summary>The configured field type, when applicable.</summary>
        public string? DataType { get; init; }
    }
}
