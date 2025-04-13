using AutoMapper;
using Georgetown_International_Academy.API.Database;
using Georgetown_Internationsl_Academy.API.Models;
using Microsoft.AspNetCore.Mvc;
using System.Net.Http.Headers;
using System.Net.Http;
using System.Text;
using Georgetown_Internationsl_Academy.API.DTO;
using System.Xml.Linq;
using Microsoft.EntityFrameworkCore;

namespace Georgetown_Internationsl_Academy.API.Services.Implimentation
{
    public class BambooHRService
    {
        private readonly ApplicationDBContext _dbContext;
        private readonly IMapper _mapper;
        private readonly ILogger<BambooHRService> _logger;
        private readonly HttpClient _httpClient;

        public BambooHRService(ApplicationDBContext dbContext, IMapper mapper, ILogger<BambooHRService> logger, IHttpClientFactory httpClientFactory)
        {
            _dbContext = dbContext;
            _mapper = mapper;
            _logger = logger;
            _httpClient = httpClientFactory.CreateClient();
        }

        public async Task<int> SyncEmployees()
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


            foreach (var emp in employees)
            {
                var existing = await _dbContext.Employees.FindAsync(emp.EmployeeCode);
                if (existing == null)
                {
                    _dbContext.Employees.Add(new Employee
                    {
                        EmployeeCode = emp.EmployeeCode,
                        FirstName = emp.FirstName,
                        LastName = emp.LastName,
                        IdDepartment = emp.IdDepartment,
                        IdDesignation = emp.IdDesignation
                    });
                }
            }

            await _dbContext.SaveChangesAsync();

            return  employees.Count;
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
                    EmployeeCode = node.Attribute("id")?.Value,
                    FirstName = node.Elements().FirstOrDefault(e => e.Attribute("id")?.Value == "firstName")?.Value,
                    LastName = node.Elements().FirstOrDefault(e => e.Attribute("id")?.Value == "lastName")?.Value,
                    IdDesignation = GetIdDesignationFromName(node.Elements().FirstOrDefault(e => e.Attribute("id")?.Value == "jobTitle")?.Value.ToString()),
                    IdDepartment = GetIdDepartmentFromName(node.Elements().FirstOrDefault(e => e.Attribute("id")?.Value == "jobTitle")?.Value),
                };

                employees.Add(emp);

            }
            return employees;
        }
            

        public int GetIdDesignationFromName(string DesignationName)
        {

            try
            {
                var desigs = _dbContext.Designations.Where(d => d.DesignationName.ToUpper() == DesignationName.ToUpper()).FirstOrDefault();
                if (desigs != null)
                {
                    return desigs.IdDesignation;
                }
                else
                    return -1;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in Getting IdDesignation from the given Designation Name.");
                throw;
            }
        }

        public int GetIdDepartmentFromName(string DepartmentName)
        {
            try
            {
                var desigs = _dbContext.Departments.Where(d => d.DepartmentName.ToUpper() == DepartmentName.ToUpper()).FirstOrDefault();
                if (desigs != null)
                {
                    return desigs.IdDepartment;
                }
                else
                    return -1;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in Getting IdDesignation from the given Designation Name.");
                throw;
            }
        }
        public bool SetEmployeePhoto()
        {

        }

        public bool CheckEmployeeAlreadyExists(string EmployeeCode)
        {
            try
            {
                var desigs = _dbContext.Employees.Where(d => d.EmployeeCode.ToUpper() == EmployeeCode.ToUpper()).FirstOrDefault();
                if (desigs != null)
                {
                    return true;
                }
                else
                    return false;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in Getting Employee Details.");
                throw;
            }
        }
    }
}
