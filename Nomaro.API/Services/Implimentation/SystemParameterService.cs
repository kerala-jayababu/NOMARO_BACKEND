using AutoMapper;
using Nomaro.API.Database;
using Nomaro.API.DTO;
using Nomaro.API.Models;
using System.Globalization;
using Nomaro.API.Services.Interface;
using Microsoft.EntityFrameworkCore;
using System.Reflection.Metadata;

namespace Nomaro.API.Services.Implimentation
{
    public class SystemParameterService: ISystemParameterService
    {
        private readonly ApplicationDBContext _dbContext;
        private readonly IMapper _mapper;
        private readonly ILogger<SystemParameterService> _logger;
        private readonly IAuditService _auditService;

        public SystemParameterService(ApplicationDBContext dbContext, IMapper mapper, ILogger<SystemParameterService> logger, IAuditService auditService)
        {
            _dbContext = dbContext;
            _mapper = mapper;
            _logger = logger;
            _auditService = auditService;
        }


        public async Task<List<SystemParameterDto>> GetAllSystemParameters()
        {
            try
            {
                var parameters = await _dbContext.SystemParameters.ToListAsync();

                if (parameters == null || !parameters.Any())
                {
                    return new List<SystemParameterDto>(); // Return an empty list if no data is found
                }

                return _mapper.Map<List<SystemParameterDto>>(parameters);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching system parameters.");
                throw;
            }
        }


        public async Task<SystemParameterDto> GetSystemParameterById(int id)
        {
            try
            {
                var parameter = await _dbContext.SystemParameters
                    .FirstOrDefaultAsync(param => param.IdSystemParameter == id);

                if (parameter == null)
                {
                    _logger.LogWarning("No system parameter found for ID: {Id}", id);
                    return null;
                }

                // Map to DTO
                return _mapper.Map<SystemParameterDto>(parameter);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching system parameter for ID: {Id}", id);
                throw;
            }
        }
        public async Task<SystemParameterDto> GetSystemParameterByName(string name)
        {
            try
            {
                var parameter = await _dbContext.SystemParameters
                    .FirstOrDefaultAsync(param => param.ParameterName == name);

                if (parameter == null)
                {
                    _logger.LogWarning("No system parameter found for Name: {Name}", name);
                    return null;
                }

                // Map entity to DTO
                return _mapper.Map<SystemParameterDto>(parameter);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching system parameter for Name: {Name}", name);
                throw;
            }
        }

        public async Task<bool> UpdateSystemParameter(SystemParameterDto dto)
        {
            try
            {
                var existingParameter = await _dbContext.SystemParameters
                    .FirstOrDefaultAsync(param => param.IdSystemParameter == dto.IdSystemParameter);

                if (existingParameter == null)
                {
                    _logger.LogWarning("No system parameter found for update with ID: {Id}", dto.IdSystemParameter);
                    return false;
                }

                // Update fields
                existingParameter.ParameterName = dto.ParameterName;
                existingParameter.ParameterDescription = dto.ParameterDescription;
                existingParameter.ParameterValue = dto.ParameterValue;
                existingParameter.ParameterBinaryValue = dto.ParameterBinaryValue;
                existingParameter.DataType = dto.DataType;
                existingParameter.ValidValues = dto.ValidValues;

                var beforeUpdate = _mapper.Map<SystemParameterDto>(existingParameter);

                _dbContext.SystemParameters.Update(existingParameter);
                await _dbContext.SaveChangesAsync();
                await _auditService.LogAuditAsync("Update", "SystemParameter", existingParameter.IdSystemParameter, new { before = beforeUpdate, after = dto });
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating system parameter with ID: {Id}", dto.IdSystemParameter);
                throw;
            }
        }



    

        // ───────────── System Parameters screen ─────────────

        private const int MaxImageBytes = 1048576; // 1 MB, same limit as SystemParameterDtoValidator
        private const int MaxValueLength = 500;
        private static readonly string[] DateFormats = { "yyyy-MM-dd", "dd-MM-yyyy", "dd/MM/yyyy", "MM/dd/yyyy", "dd-MMM-yyyy" };
        private static readonly string[] TrueWords = { "true", "1", "yes", "y" };
        private static readonly string[] FalseWords = { "false", "0", "no", "n" };

        public async Task<List<SystemParameterConfigDto>> GetConfigurableSystemParameters()
        {
            try
            {
                return await _dbContext.SystemParameters.AsNoTracking()
                    .Where(p => p.ShowInUIToConfigure == true)
                    .OrderBy(p => p.ParameterName)
                    .Select(p => new SystemParameterConfigDto
                    {
                        IdSystemParameter = p.IdSystemParameter,
                        ParameterName = p.ParameterName,
                        ParameterDescription = p.ParameterDescription,
                        ParameterValue = p.ParameterValue,
                        DataType = p.DataType,
                        ValidValues = p.ValidValues,
                        ParameterValueEditable = p.ParameterValueEditable == true,
                        ParameterBinaryValueEditable = p.ParameterBinaryValueEditable == true
                    })
                    .ToListAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching configurable system parameters.");
                throw;
            }
        }

        public async Task<SystemParameterImageDto?> GetSystemParameterImage(int id)
        {
            try
            {
                // Reads the image of this one parameter only
                var parameter = await _dbContext.SystemParameters.AsNoTracking()
                    .Where(p => p.IdSystemParameter == id && p.ShowInUIToConfigure == true)
                    .Select(p => new { p.IdSystemParameter, p.ParameterName, p.ParameterBinaryValue })
                    .FirstOrDefaultAsync();
                if (parameter?.ParameterBinaryValue == null || parameter.ParameterBinaryValue.Length == 0)
                    return null;

                return new SystemParameterImageDto
                {
                    IdSystemParameter = parameter.IdSystemParameter,
                    ParameterName = parameter.ParameterName,
                    ContentType = DetectImageType(parameter.ParameterBinaryValue) ?? "image/png",
                    ImageBase64 = Convert.ToBase64String(parameter.ParameterBinaryValue),
                    ImageSizeBytes = parameter.ParameterBinaryValue.Length
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching image of system parameter {Id}", id);
                throw;
            }
        }

        public async Task UpdateSystemParameterValue(SystemParameterValueUpdateDto dto)
        {
            // Everything except the image column (the image is only written, never read, here)
            var parameter = await _dbContext.SystemParameters.AsNoTracking()
                .Where(p => p.IdSystemParameter == dto.IdSystemParameter && p.ShowInUIToConfigure == true)
                .Select(p => new SystemParameter
                {
                    IdSystemParameter = p.IdSystemParameter,
                    ParameterName = p.ParameterName,
                    ParameterValue = p.ParameterValue,
                    DataType = p.DataType,
                    ValidValues = p.ValidValues,
                    ParameterValueEditable = p.ParameterValueEditable,
                    ParameterBinaryValueEditable = p.ParameterBinaryValueEditable
                })
                .FirstOrDefaultAsync();
            if (parameter == null)
                throw new InvalidOperationException("System parameter not found.");

            var valueEditable = parameter.ParameterValueEditable == true;
            var imageEditable = parameter.ParameterBinaryValueEditable == true;
            if (!valueEditable && !imageEditable)
                throw new InvalidOperationException($"{parameter.ParameterName} cannot be changed.");

            var oldValue = parameter.ParameterValue;
            string? newValue = null;
            byte[]? newImage = null;

            if (valueEditable && dto.ParameterValue != null)
            {
                var checkedValue = ValidateParameterValue(parameter, dto.ParameterValue);
                if (checkedValue != oldValue)
                    newValue = checkedValue;
            }

            if (dto.ImageFile != null && dto.ImageFile.Length > 0)
            {
                if (!imageEditable)
                    throw new InvalidOperationException($"The image of {parameter.ParameterName} cannot be changed.");
                if (dto.ImageFile.Length > MaxImageBytes)
                    throw new InvalidOperationException("Image must not be larger than 1 MB.");

                using var stream = new MemoryStream();
                await dto.ImageFile.CopyToAsync(stream);
                var bytes = stream.ToArray();
                if (DetectImageType(bytes) == null)
                    throw new InvalidOperationException("Select a PNG, JPG, GIF, BMP or WEBP image.");

                newImage = bytes;
            }

            if (newValue == null && newImage == null)
                throw new InvalidOperationException("Nothing to update.");

            try
            {
                // Update only the changed column(s) without loading the row
                var query = _dbContext.SystemParameters.Where(p => p.IdSystemParameter == parameter.IdSystemParameter);
                if (newValue != null)
                    await query.ExecuteUpdateAsync(s => s.SetProperty(p => p.ParameterValue, newValue));
                if (newImage != null)
                    await query.ExecuteUpdateAsync(s => s.SetProperty(p => p.ParameterBinaryValue, newImage));

                await _auditService.LogAuditAsync("Update", "SystemParameter", parameter.IdSystemParameter, new
                {
                    before = new { ParameterValue = oldValue },
                    after = new { ParameterValue = newValue ?? oldValue, ImageChanged = newImage != null, ImageSizeBytes = newImage?.Length }
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating system parameter {Id}", dto.IdSystemParameter);
                throw;
            }
        }

        /// <summary>
        /// Checks the value against the parameter's DataType and ValidValues and returns the value to store.
        /// ValidValues can be a list (comma, semicolon or | separated) or, for numbers, a range such as 1-31 or 1..31.
        /// </summary>
        private static string ValidateParameterValue(SystemParameter parameter, string input)
        {
            var name = parameter.ParameterName ?? "Value";
            var value = input.Trim();
            var type = NormaliseDataType(parameter.DataType);

            if (value.Length > MaxValueLength)
                throw new InvalidOperationException($"{name} must not exceed {MaxValueLength} characters.");

            if (value.Length == 0)
            {
                if (type == "STRING" && string.IsNullOrWhiteSpace(parameter.ValidValues))
                    return value;
                throw new InvalidOperationException($"Enter a value for {name}.");
            }

            var range = ParseRange(parameter.ValidValues, type);
            var list = range == null ? ParseList(parameter.ValidValues) : new List<string>();

            switch (type)
            {
                case "INTEGER":
                    if (!long.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out var whole))
                        throw new InvalidOperationException($"{name} must be a whole number.");
                    CheckRange(name, whole, range);
                    break;
                case "DECIMAL":
                    if (!decimal.TryParse(value, NumberStyles.Number, CultureInfo.InvariantCulture, out var number))
                        throw new InvalidOperationException($"{name} must be a number.");
                    CheckRange(name, number, range);
                    break;
                case "BOOLEAN":
                    var lower = value.ToLowerInvariant();
                    if (list.Count == 0 && !TrueWords.Contains(lower) && !FalseWords.Contains(lower))
                        throw new InvalidOperationException($"{name} must be Yes or No.");
                    break;
                case "DATE":
                    if (!DateTime.TryParseExact(value, DateFormats, CultureInfo.InvariantCulture, DateTimeStyles.None, out _))
                        throw new InvalidOperationException($"{name} must be a valid date.");
                    break;
                case "EMAIL":
                    if (!System.Text.RegularExpressions.Regex.IsMatch(value, @"^[^@\s]+@[^@\s]+\.[^@\s]+$"))
                        throw new InvalidOperationException($"{name} must be a valid email address.");
                    break;
            }

            if (list.Count > 0)
            {
                var match = list.FirstOrDefault(v => string.Equals(v, value, StringComparison.OrdinalIgnoreCase));
                if (match == null)
                    throw new InvalidOperationException($"{name} must be one of: {string.Join(", ", list)}.");
                return match; // keep the casing used in ValidValues
            }

            return value;
        }

        private static string NormaliseDataType(string? dataType)
        {
            var t = (dataType ?? string.Empty).Trim().ToUpperInvariant();
            return t switch
            {
                "INT" or "INTEGER" or "BIGINT" or "SMALLINT" or "NUMBER" or "LONG" => "INTEGER",
                "DECIMAL" or "NUMERIC" or "FLOAT" or "DOUBLE" or "MONEY" or "CURRENCY" or "PERCENTAGE" => "DECIMAL",
                "BOOL" or "BOOLEAN" or "BIT" or "YESNO" or "YES/NO" => "BOOLEAN",
                "DATE" or "DATETIME" => "DATE",
                "EMAIL" => "EMAIL",
                _ => "STRING"
            };
        }

        private static List<string> ParseList(string? validValues) =>
            string.IsNullOrWhiteSpace(validValues)
                ? new List<string>()
                : validValues.Split(new[] { ',', ';', '|' }, StringSplitOptions.RemoveEmptyEntries)
                    .Select(v => v.Trim()).Where(v => v.Length > 0).Distinct().ToList();

        private static (decimal Min, decimal Max)? ParseRange(string? validValues, string type)
        {
            if (string.IsNullOrWhiteSpace(validValues) || (type != "INTEGER" && type != "DECIMAL")) return null;
            var m = System.Text.RegularExpressions.Regex.Match(validValues.Trim(),
                @"^(-?\d+(?:\.\d+)?)\s*(?:-|\.\.|to)\s*(-?\d+(?:\.\d+)?)$", System.Text.RegularExpressions.RegexOptions.IgnoreCase);
            if (!m.Success) return null;
            var min = decimal.Parse(m.Groups[1].Value, CultureInfo.InvariantCulture);
            var max = decimal.Parse(m.Groups[2].Value, CultureInfo.InvariantCulture);
            return min <= max ? (min, max) : (max, min);
        }

        private static void CheckRange(string name, decimal value, (decimal Min, decimal Max)? range)
        {
            if (range.HasValue && (value < range.Value.Min || value > range.Value.Max))
                throw new InvalidOperationException($"{name} must be between {range.Value.Min} and {range.Value.Max}.");
        }

        /// <summary>Content type from the file's first bytes; null when it is not a supported image.</summary>
        private static string? DetectImageType(byte[] bytes)
        {
            if (bytes.Length >= 8 && bytes[0] == 0x89 && bytes[1] == 0x50 && bytes[2] == 0x4E && bytes[3] == 0x47) return "image/png";
            if (bytes.Length >= 3 && bytes[0] == 0xFF && bytes[1] == 0xD8 && bytes[2] == 0xFF) return "image/jpeg";
            if (bytes.Length >= 6 && bytes[0] == 0x47 && bytes[1] == 0x49 && bytes[2] == 0x46) return "image/gif";
            if (bytes.Length >= 2 && bytes[0] == 0x42 && bytes[1] == 0x4D) return "image/bmp";
            if (bytes.Length >= 12 && bytes[0] == 0x52 && bytes[1] == 0x49 && bytes[2] == 0x46 && bytes[3] == 0x46 &&
                bytes[8] == 0x57 && bytes[9] == 0x45 && bytes[10] == 0x42 && bytes[11] == 0x50) return "image/webp";
            return null;
        }
    }
}
