using Asp.Versioning;
using FluentValidation;
using Nomaro.API.DTO;
using Nomaro.API.Models;
using Nomaro.API.Services.Implementation;
using Nomaro.API.Services.Implimentation;
using Nomaro.API.Services.Interface;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Net.Http.Headers;
using System.Security.Claims;
using System.Text;
using System.Xml.Linq;

namespace Nomaro.API.Controllers
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
        public async Task<IActionResult> SyncTimeOffRequestsForLeave([FromQuery] DateTime? start, [FromQuery] DateTime? end)
        {
            try
            {
                var lastRunDate = await _bambooservice.BambooHRLeaveIntegrationLastRun();
              
                var fromDate = start ?? lastRunDate ?? DateTime.UtcNow;

                // If end passed → override
                // else use today
                var toDate = end ?? DateTime.UtcNow;

                var result = await _bambooservice.SyncTimeOffRequestsForLeave(fromDate, toDate);
             
                return Ok(result); // Return success message
            }
            catch (Exception ex)
            {
             
                return StatusCode(500, $"Internal server error: {ex.Message}");
            }
        }


    }

}

