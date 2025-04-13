using Georgetown_International_Academy.API.Database;
using Georgetown_Internationsl_Academy.API.DTO;
using Georgetown_Internationsl_Academy.API.Helpers;
using Georgetown_Internationsl_Academy.API.Models;
using Georgetown_Internationsl_Academy.API.Services.Interface;
using iText.Kernel.Pdf.Canvas.Wmf;
using Microsoft.EntityFrameworkCore;
using Org.BouncyCastle.Asn1.Cmp;
using Org.BouncyCastle.Asn1.Crmf;
using RestSharp;
using System.Xml.Serialization;

namespace Georgetown_Internationsl_Academy.API.Services.Implimentation
{
    public class BambooServices : IBambooServices
    {
        private readonly IConfiguration _configuration;
        private readonly ILogger<BambooServices> _logger;
        private readonly ApplicationDBContext _dbContext;

        public BambooServices(IConfiguration configuration, ILogger<BambooServices> logger, ApplicationDBContext dbContext)
        {
            _configuration = configuration;
            _logger = logger;
            _dbContext = dbContext;
        }

        public async Task<List<BambooHRDetailsDto>> SyncEmployeesFromBambooHR()
        {
            var result = new List<BambooHRDetailsDto>();
            try
            {
                var baseURL = _configuration["BambooHR:BaseUrl"];
                var apikey = _configuration["BambooHR:ApiKey"];
                var photoSavePath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "photos");

                if (string.IsNullOrWhiteSpace(baseURL) || string.IsNullOrWhiteSpace(apikey))
                {
                    _logger.LogError("BambooHR BaseUrl or ApiKey is missing in configuration.");
                    return new List<BambooHRDetailsDto>();
                }

                if (!Directory.Exists(photoSavePath))
                    Directory.CreateDirectory(photoSavePath);

                var fullUrl = $"{baseURL}/employees/directory";
                var client = new RestClient();
                var request = new RestRequest(fullUrl, Method.Get);

                var token = Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes($"{apikey}:x"));
                request.AddHeader("Authorization", $"Basic {token}");

                RestResponse response = await client.ExecuteAsync(request);

                if (!response.IsSuccessful)
                {
                    _logger.LogError("Failed to retrieve data from BambooHR. Status: {StatusCode}, Content: {Content}",
                        response.StatusCode, response.Content);
                    return new List<BambooHRDetailsDto>();
                }

                var serializer = new XmlSerializer(typeof(EmployeeDirectoryDto));
                using var reader = new StringReader(response.Content);
                var directory = (EmployeeDirectoryDto)serializer.Deserialize(reader);

                var rawEmployees = directory?.Employees.Take(2) ?? new List<EmployeeRawDto>();

                foreach (var emp in rawEmployees)
                {
                    try
                    {
                        var detailUrl = $"{baseURL}/employees/{emp.Id}?fields=displayName,firstName,LastName,gender,dateofBirth,address1,address2,middleName,workPhone,mobilePhone,city,state,zipcode,JoiningDate,commissionDate,supervisor,status,terminationDate,department,jobTitle,workEmail";

                        var detailRequest = new RestRequest(detailUrl, Method.Get);
                        detailRequest.AddHeader("Authorization", $"Basic {token}");

                        var detailResponse = await client.ExecuteAsync(detailRequest);
                        if (!detailResponse.IsSuccessful)
                        {
                            _logger.LogWarning("Failed to get details for employee ID {Id}", emp.Id);
                            continue;
                        }

                        var detailSerializer = new XmlSerializer(typeof(BambooHREmployeeXmlDto));
                        using var detailReader = new StringReader(detailResponse.Content);
                        var detailedRaw = (BambooHREmployeeXmlDto)detailSerializer.Deserialize(detailReader);

                        var mapped = BambooEmployeeMapper.ToDetailsDto(detailedRaw);
                        await AddUpdateEmployeeDetailsFromBambooHR(mapped);
                        result.Add(mapped);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Error while processing employee ID {Id}", emp.Id);
                    }
                }


                return result;

               


            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error syncing employees from BambooHR");
                return new List<BambooHRDetailsDto>();
            }
        }

        public async Task<bool> AddUpdateEmployeeDetailsFromBambooHR(BambooHRDetailsDto bambooEmp)
        {

            var empDetails = await _dbContext.Employees.Where(em => em.EmployeeCode == bambooEmp.Id).FirstOrDefaultAsync();
            if (empDetails == null)
                empDetails = new Models.Employee();
            else
                empDetails.IdEmployee = empDetails.IdEmployee;

            empDetails.EmployeeCode = bambooEmp.Id;
            empDetails.FirstName = bambooEmp.FirstName;
            empDetails.MiddleName = bambooEmp.MiddleName;
            empDetails.LastName = bambooEmp.LastName;
            empDetails.Gender = bambooEmp.Gender;

            var idDesignation = GetIdDesignation(bambooEmp.JobTitle);
            if (await idDesignation > 0)
                empDetails.IdDesignation = await idDesignation;

            var idDepartment = GetIdDepartment(bambooEmp.Department);
            if (await idDepartment > 0)
                empDetails.IdDepartment = await idDepartment;

            empDetails.EmailID = bambooEmp.WorkEmail;
            empDetails.PhoneNumber1 = bambooEmp.WorkPhone;
            empDetails.PhoneNumber2 = bambooEmp.MobilePhone;
            empDetails.Address1 = bambooEmp.Address1;
            empDetails.Address2 = bambooEmp.Address2;
            empDetails.City = bambooEmp.City;
            empDetails.State = bambooEmp.State;
            empDetails.ZipCode = bambooEmp.ZipCode;

            var reportingTo = GetIdEmployee(bambooEmp.Supervisor);
            if (await reportingTo > 0)
                empDetails.ReportingTo = await reportingTo;
            empDetails.CurrentStatus = bambooEmp.Status == "Active" ? "Working" : "NotWorking";
            if(bambooEmp.TerminationDate != null)
                empDetails.LastWorkingDay = bambooEmp.TerminationDate;

            if (empDetails.IdEmployee > 0)
                _dbContext.Employees.Update(empDetails);
            else
                _dbContext.Employees.Add(empDetails);

             await _dbContext.SaveChangesAsync();

            return true;
        }

        public async Task<int> GetIdDesignation(string DesignationName)
        {
            var desigDetails = await _dbContext.Designations.Where(dd => dd.DesignationName.ToUpper() == DesignationName.ToUpper()).FirstOrDefaultAsync();
            if (desigDetails != null)
                return desigDetails.IdDesignation;
                
            return -1;
        }

        public async Task<int> GetIdDepartment(string DepartmentName)
        {
            var deptDetails = await _dbContext.Departments.Where(dd => dd.DepartmentName.ToUpper() == DepartmentName.ToUpper()).FirstOrDefaultAsync();
            if (deptDetails != null)
                return deptDetails.IdDepartment;

            return -1;
        }

        public async Task<int> GetIdEmployee(string EmployeeName)
        {
            string[] splitEName = EmployeeName.Split(',');
            if (splitEName.Length == 1)
            {
                var empDetails = await _dbContext.Employees.Where(em => (em.LastName ?? "").ToUpper() == splitEName[0].Trim().ToUpper()).FirstOrDefaultAsync();
                if (empDetails != null)
                    return empDetails.IdEmployee;
            }
            if (splitEName.Length == 2)
            {
                var empDetails = await _dbContext.Employees.Where(em => (em.LastName ?? "").ToUpper() == splitEName[0].Trim().ToUpper() && 
                em.FirstName == splitEName[1].ToUpper()).FirstOrDefaultAsync();
                if (empDetails != null)
                    return empDetails.IdEmployee;
            }
            return -1;
        }

    }

}
