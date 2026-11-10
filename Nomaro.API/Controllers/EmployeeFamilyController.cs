using Asp.Versioning;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Nomaro.API.DTO;
using Nomaro.API.Helpers;
using Nomaro.API.Services.Interface;
using System.Security.Claims;

namespace Nomaro.API.Controllers
{
    /// <summary>Employee Management -> Family Members tab. Permissions from Employee Profile (EMPPROF).</summary>
    [ApiController]
    [ApiVersion(1)]
    [Authorize]
    [Route("/api/v{v:apiVersion}/[controller]")]
    public class EmployeeFamilyController : ControllerBase
    {
        private readonly IEmployeeFamilyService _service;
        private readonly IRoleBasedScreenService _roleBasedService;
        private readonly IConfiguration _configuration;

        public EmployeeFamilyController(IEmployeeFamilyService service, IRoleBasedScreenService roleBasedService, IConfiguration configuration)
        {
            _service = service;
            _roleBasedService = roleBasedService;
            _configuration = configuration;
        }

        private async Task<(int? me, IActionResult? denied)> Check(string action)
        {
            if (!int.TryParse(HttpContext.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value, out var me))
                return (null, Unauthorized(ApiResponseDto<string>.CreateFailure("Employee ID not found.")));
            var ok = await _roleBasedService.CheckEmployeePermission(me, _configuration["ScreenCodes:EmployeeProfile"], action);
            return ok ? (me, null) : (null, StatusCode(403, ApiResponseDto<string>.CreateFailure("You do not have the required permission to perform this action.")));
        }

        private IActionResult Fail(Exception ex) => ex switch
        {
            KeyNotFoundException => NotFound(ApiResponseDto<string>.CreateFailure(ex.Message)),
            InvalidOperationException => BadRequest(ApiResponseDto<string>.CreateFailure(ex.Message)),
            _ => StatusCode(500, ApiResponseDto<string>.CreateFailure($"An error occurred: {ex.Message}"))
        };

        [HttpGet("GetRelationships")]
        public IActionResult GetRelationships() =>
            Ok(ApiResponseDto<object>.CreateSuccess(FamilyRelationships.All.Select(r => new { value = r.Code, label = r.Label, gender = r.Gender, single = r.Single }), "OK"));

        [HttpGet("GetFamilyMembers")]
        public async Task<IActionResult> GetFamilyMembers(int idEmployee)
        {
            var (_, denied) = await Check("V");
            if (denied != null) return denied;
            try { return Ok(ApiResponseDto<List<EmployeeFamilyMemberDto>>.CreateSuccess(await _service.GetFamilyMembers(idEmployee), "OK")); }
            catch (Exception ex) { return Fail(ex); }
        }

        [HttpPost("AddOrUpdateFamilyMember")]
        public async Task<IActionResult> AddOrUpdateFamilyMember([FromBody] EmployeeFamilyMemberDto dto)
        {
            var (me, denied) = await Check(dto?.IdEmployeeFamilyMember > 0 ? "U" : "A");
            if (denied != null) return denied;
            try
            {
                var id = await _service.AddOrUpdateFamilyMember(dto!, me!.Value);
                return Ok(ApiResponseDto<int>.CreateSuccess(id, dto!.IdEmployeeFamilyMember > 0 ? "Family member updated." : "Family member added."));
            }
            catch (Exception ex) { return Fail(ex); }
        }

        [HttpDelete("DeleteFamilyMember")]
        public async Task<IActionResult> DeleteFamilyMember(int id)
        {
            var (me, denied) = await Check("D");
            if (denied != null) return denied;
            try
            {
                await _service.DeleteFamilyMember(id, me!.Value);
                return Ok(ApiResponseDto<string>.CreateSuccess("Deleted", "Family member deleted."));
            }
            catch (Exception ex) { return Fail(ex); }
        }
    }
}
