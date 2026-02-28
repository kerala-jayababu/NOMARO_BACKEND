using Asp.Versioning;
using FluentValidation;
using Georgetown_Internationsl_Academy.API.DTO;
using Georgetown_Internationsl_Academy.API.Models;
using Georgetown_Internationsl_Academy.API.Services.Implementation;
using Georgetown_Internationsl_Academy.API.Services.Implimentation;
using Georgetown_Internationsl_Academy.API.Services.Interface;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Net.Http.Headers;
using System.Security.Claims;
using System.Text;
using System.Xml.Linq;

namespace Georgetown_Internationsl_Academy.API.Controllers
{
    [ApiController]
    [ApiVersion(1)]
    [Route("/api/v{v:apiVersion}/[controller]")]
 

    public class BambooController : ControllerBase
    {             


        private readonly IBambooServices _bambooservice;        
        private readonly IConfiguration _configuration;
        public BambooController(IBambooServices bambooservice,  IConfiguration configuration)
        {
            _bambooservice = bambooservice;;
            _configuration = configuration;
        }


        [HttpGet("SyncEmployeesFromBambooHR")]
        public async Task<IActionResult> SyncEmployeesFromBambooHR()
        {

            try
            {

            var result = await _bambooservice.SyncEmployeesFromBambooHR();
                return Ok(result);

            }
            catch (Exception ex)
            {
                return StatusCode(500, $"An error occurred: {ex.Message}");
            }

        }

        [HttpGet("SyncTimeOffRequests")]
        public async Task<IActionResult> SyncTimeOffRequests([FromQuery] DateTime? start = null, [FromQuery] DateTime? end = null)
        {
            try
            {
                var lastRunDate = await _bambooservice.BambooHRLeaveIntegrationLastRun();
                var fromDate = start ?? lastRunDate ?? DateTime.UtcNow;
                var toDate = end ?? DateTime.Today;

                var result = await _bambooservice.SyncTimeOffRequests(fromDate, toDate);
                return Ok(result);
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Error syncing time off requests: {ex.Message}");
            }
        }
        [HttpGet("SyncTimeOffRequestsForLeave")]
        public async Task<IActionResult> SyncTimeOffRequestsForLeave([FromQuery] DateTime start, [FromQuery] DateTime end)
        {
            try
            {
                var result = await _bambooservice.SyncTimeOffRequestsForLeave(start, end);
                return Ok(result); // Return success message
            }
            catch (Exception ex)
            {
             
                return StatusCode(500, $"Internal server error: {ex.Message}");
            }
        }


    }


}
