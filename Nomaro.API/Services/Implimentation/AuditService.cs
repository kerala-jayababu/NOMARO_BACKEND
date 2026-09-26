using Nomaro.API.Database;
using Nomaro.API.DTO;
using Nomaro.API.Models;
using Nomaro.API.Services.Interface;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using System.Text.Json;


namespace Nomaro.API.Services.Implimentation
{
    public class AuditService : IAuditService
    {
        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly IConfiguration _configuration;

        // JSON serializer options — camelCase, ignore nulls
        private readonly ApplicationDBContext _db;

        // JSON serializer options — camelCase, ignore nulls
        private static readonly JsonSerializerOptions _jsonOptions = new JsonSerializerOptions
        {
            WriteIndented = false,
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            DefaultIgnoreCondition = System.Text.Json.Serialization
                                         .JsonIgnoreCondition.WhenWritingNull
        };

        public AuditService(
            ApplicationDBContext db,
            IHttpContextAccessor httpContextAccessor)
        {
            _db = db;
            _httpContextAccessor = httpContextAccessor;
        }

        /// <summary>
        /// Logs an audit record for Create / Update / Delete operations.
        /// Automatically captures: UserId, IP Address, Device Info, Timestamp.
        /// </summary>
        public async Task LogAuditAsync(
            string actionType,
            string entityName,
            int entityId,
            object actionDetails)
        {
            try
            {
                var ctx = _httpContextAccessor.HttpContext;

                // ── Extract logged-in user ID from JWT Claims ──────────────
                var userId = ctx?.User?.FindFirstValue(ClaimTypes.NameIdentifier)
                          ?? ctx?.User?.FindFirstValue("sub")          // JWT 'sub'
                          ?? ctx?.User?.FindFirstValue(ClaimTypes.Name)
                          ?? "System";

                // ── Extract Client IP Address ──────────────────────────────
                // Handles reverse-proxy / load-balancer forwarded headers
                var ip = ctx?.Request?.Headers["X-Forwarded-For"].ToString();
                if (string.IsNullOrWhiteSpace(ip))
                    ip = ctx?.Connection?.RemoteIpAddress?.ToString() ?? "Unknown";

                // Take only the first IP if comma-separated list
                if (ip.Contains(","))
                    ip = ip.Split(',')[0].Trim();

                // ── Extract Device / Browser Info ──────────────────────────
                var userAgent = ctx?.Request?.Headers["User-Agent"].ToString()
                             ?? "Unknown";

                // Truncate to 500 chars to fit column
                if (userAgent.Length > 500)
                    userAgent = userAgent.Substring(0, 500);

                // ── Serialize ActionDetails to JSON ────────────────────────
                var detailsJson = actionDetails != null
                    ? JsonSerializer.Serialize(actionDetails, _jsonOptions)
                    : null;

                // ── Build and save AuditLog record ─────────────────────────
                var audit = new AuditLogs
                {
                    ActionType = actionType,
                    EntityName = entityName,
                    EntityId = entityId,
                    ActionBy = userId,
                    ActionTimestamp = DateTime.UtcNow,
                    ActionDetails = detailsJson,
                    IPAddress = ip,
                    DeviceInfo = userAgent
                };

                await _db.AuditLog.AddAsync(audit);
                await _db.SaveChangesAsync();
            }
            catch (Exception ex)
            {
                // IMPORTANT: Never let audit failure break the main operation.
                // Log to console/file. Replace with your logger if available.
                Console.Error.WriteLine($"[AuditService] Failed to write audit log: {ex.Message}");
            }
        }
    }
}

