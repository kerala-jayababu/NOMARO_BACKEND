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
using System.Net.Http.Headers;
using System.Security.Claims;
using System.Text;
using System.Xml.Linq;

namespace Georgetown_Internationsl_Academy.API.Controllers
{
    [ApiController]
    [ApiVersion(1)]
    [Route("/api/v{v:apiVersion}/[controller]")]
    [Authorize]

    public class BambooController : ControllerBase
    {
        private readonly HttpClient _httpClient;

        public BambooController(IHttpClientFactory httpClientFactory)
        {
            _httpClient = httpClientFactory.CreateClient();
        }
        public async Task<IActionResult> SyncEmployeesFromBambooHR()
        {
            var bambooUrl = "https://api.bamboohr.com/api/gateway.php/giagy/v1/employees/directory";
            var apiKey = "57173c7724147db4c9c2951f7027c142f68210c6";
            var basicAuth = Convert.ToBase64String(Encoding.ASCII.GetBytes($"{apiKey}:x"));

            _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Basic", basicAuth);

            var response = await _httpClient.GetAsync(bambooUrl);

            if (!response.IsSuccessStatusCode)
                return StatusCode((int)response.StatusCode, await response.Content.ReadAsStringAsync());

            var xml = await response.Content.ReadAsStringAsync();

            var employees = ParseEmployeeXml(xml);

            using var scope = HttpContext.RequestServices.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<YourDbContext>();

            foreach (var emp in employees)
            {
                var existing = await db.Employees.FindAsync(emp.Id);
                if (existing == null)
                {
                    db.Employees.Add(new Employee
                    {
                        Id = emp.Id,
                        DisplayName = emp.DisplayName,
                        FirstName = emp.FirstName,
                        LastName = emp.LastName,
                        PreferredName = emp.PreferredName,
                        JobTitle = emp.JobTitle,
                        Pronouns = emp.Pronouns,
                        PhotoUploaded = emp.PhotoUploaded,
                        PhotoUrl = emp.PhotoUrl,
                        CanUploadPhoto = emp.CanUploadPhoto
                    });
                }
                else
                {
                    existing.DisplayName = emp.DisplayName;
                    existing.FirstName = emp.FirstName;
                    existing.LastName = emp.LastName;
                    existing.PreferredName = emp.PreferredName;
                    existing.JobTitle = emp.JobTitle;
                    existing.Pronouns = emp.Pronouns;
                    existing.PhotoUploaded = emp.PhotoUploaded;
                    existing.PhotoUrl = emp.PhotoUrl;
                    existing.CanUploadPhoto = emp.CanUploadPhoto;
                }
            }

            await db.SaveChangesAsync();

            return Ok(new { Count = employees.Count });
        }

        private List<EmployeeDto> ParseEmployeeXml(string xmlContent)
        {
            var doc = XDocument.Parse(xmlContent);
            var employees = new List<EmployeeDto>();

            var employeeNodes = doc.Descendants("employee");

            foreach (var node in employeeNodes)
            {
                var emp = new EmployeeDto
                {
                    Id = node.Attribute("id")?.Value,
                    DisplayName = node.Elements().FirstOrDefault(e => e.Attribute("id")?.Value == "displayName")?.Value,
                    FirstName = node.Elements().FirstOrDefault(e => e.Attribute("id")?.Value == "firstName")?.Value,
                    LastName = node.Elements().FirstOrDefault(e => e.Attribute("id")?.Value == "lastName")?.Value,
                    PreferredName = node.Elements().FirstOrDefault(e => e.Attribute("id")?.Value == "preferredName")?.Value,
                    JobTitle = node.Elements().FirstOrDefault(e => e.Attribute("id")?.Value == "jobTitle")?.Value,
                    Pronouns = node.Elements().FirstOrDefault(e => e.Attribute("id")?.Value == "pronouns")?.Value,
                    PhotoUploaded = node.Elements().FirstOrDefault(e => e.Attribute("id")?.Value == "photoUploaded")?.Value == "true",
                    PhotoUrl = node.Elements().FirstOrDefault(e => e.Attribute("id")?.Value == "photoUrl")?.Value,
                    CanUploadPhoto = node.Elements().FirstOrDefault(e => e.Attribute("id")?.Value == "canUploadPhoto")?.Value,
                };

                employees.Add(emp);
            }

            return employees;
        }

    }


}
