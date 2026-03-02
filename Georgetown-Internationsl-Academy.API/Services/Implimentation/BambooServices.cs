using Georgetown_International_Academy.API.Database;
using Georgetown_Internationsl_Academy.API.DTO;
using Georgetown_Internationsl_Academy.API.DTO.Time___Attendance.Shift;
using Georgetown_Internationsl_Academy.API.Helpers;
using Georgetown_Internationsl_Academy.API.Models;
using Georgetown_Internationsl_Academy.API.Models.Time___Attendance.Bamboo_HR;
using Georgetown_Internationsl_Academy.API.Models.Time___Attendance.Shift;
using Georgetown_Internationsl_Academy.API.Services.Implimentation.Time___Attendance.BambooHR;
using Georgetown_Internationsl_Academy.API.Services.Interface;
using iText.Kernel.Pdf.Canvas.Wmf;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.SqlServer.Query.Internal;
using Newtonsoft.Json;
using Org.BouncyCastle.Asn1;
using Org.BouncyCastle.Asn1.Cmp;
using Org.BouncyCastle.Asn1.Crmf;
using RestSharp;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Xml.Serialization;

namespace Georgetown_Internationsl_Academy.API.Services.Implimentation
{
    public class BambooServices : IBambooServices
    {
        private readonly IConfiguration _configuration;
        private readonly ILogger<BambooServices> _logger;
        private readonly ApplicationDBContext _dbContext;
        private bool IsDataChangedInBambooHR;
        private string DataChanges;
        private bool IsNewEmployee;
        private string DesignationName;
        private string DepartmentName;
        private string ReportingTo;
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
                IsDataChangedInBambooHR = false;
                var baseURL = _configuration["BambooHR:BaseUrl"];
                var apikey = _configuration["BambooHR:ApiKey"];

                if (string.IsNullOrWhiteSpace(baseURL) || string.IsNullOrWhiteSpace(apikey))
                {
                    _logger.LogError("BambooHR BaseUrl or ApiKey is missing in configuration.");
                    return new List<BambooHRDetailsDto>();
                }
            
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

                var rawEmployees = directory?.Employees ?? new List<EmployeeRawDto>();

                await UpdateEmployeeStatusNotWorkingIfNotFoundInBambooHR(rawEmployees);

                foreach (var emp in rawEmployees)
                {
                    try
                    {
                        var detailUrl = $"{baseURL}/employees/{emp.Id}?fields=displayName,firstName,LastName,gender,dateofBirth,address1,address2,middleName,workPhone,mobilePhone,city,state,zipcode,JoiningDate,commissionDate,supervisor,status,terminationDate,department,jobTitle,workEmail,hiredate,employeenumber,customNIS,customTIN";

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
                        mapped.EmployeePhotoPath = emp.Fields.FirstOrDefault(f => f.Id == "photoUrl")?.Value;
                        if (mapped.EmployeeNumber.IndexOf("0000") < 0)
                        {
                            await AddUpdateEmployeeDetailsFromBambooHR(mapped);
                        }
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



        
        public async Task<DateTime?> BambooHRLeaveIntegrationLastRun()
        {
            try
            {
                var lastexecDate = await _dbContext.BambooHRLeaveIntegrationLastRun.Select(x => x.LeaveIntegrationLastRunDate).FirstOrDefaultAsync();
                if (lastexecDate == DateTime.MinValue)
                    return null;
                return lastexecDate;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error  BambooHRLeaveIntegrationLastRun from BambooHR");
                return null;
            }

        }
        public async Task<string> SyncTimeOffRequests(DateTime start, DateTime end)
        {
            // Initialize BambooHR API settings
            var baseUrl = "https://api.bamboohr.com";
            var apiKey = _configuration["BambooHR:ApiKey"];
            var subdomain = _configuration["BambooHR:Subdomain"];

            var client = new RestClient(new RestClientOptions(baseUrl) { MaxTimeout = -1 });
            var url = $"/api/gateway.php/{subdomain}/v1/time_off/requests/?start={start:yyyy-MM-dd}&end={end:yyyy-MM-dd}";
            var request = new RestRequest(url, Method.Get);
            var token = Convert.ToBase64String(Encoding.UTF8.GetBytes($"{apiKey}:x"));
            request.AddHeader("Authorization", $"Basic {token}");

            // Make API request
            var response = await client.ExecuteAsync(request);
            if (!response.IsSuccessful)
                throw new Exception($"BambooHR API failed: {response.StatusCode} - {response.Content}");

            // Deserialize XML response
            var serializer = new XmlSerializer(typeof(TimeOffRequestsDto));
            using var reader = new StringReader(response.Content);
            var requests = serializer.Deserialize(reader) as TimeOffRequestsDto;

            if (requests?.Requests == null || !requests.Requests.Any())
                return "No requests found in the date range.";

            // Prepare in-memory collections
            var employeeMap = new Dictionary<int, int>();                        // BambooId => InternalId
            var logsToInsert = new List<BambooHRIntegrationLogs>();
            var leavesToInsert = new List<EmployeeLeave>();
            var leaveDetailsToInsert = new List<EmployeeLeaveDetail>();

            // Start DB transaction
            using var transaction = await _dbContext.Database.BeginTransactionAsync();

            try
            {
                // Process each time-off request
                foreach (var req in requests.Requests)
                {
                    int internalEmpId;

                    // Try to reuse already mapped employee
                    if (employeeMap.ContainsKey(req.Employee.Id))
                    {
                        internalEmpId = employeeMap[req.Employee.Id];
                    }
                    else
                    {
                        try
                        {
                            internalEmpId = await GetInternalEmployeeIdFromBambooId(req.Employee.Id);
                            employeeMap[req.Employee.Id] = internalEmpId;
                        }
                        catch
                        {
                            // Log missing employee
                            var rawXml = await GetRawEmployeeXml(req.Employee.Id);
                            var mapped = BambooEmployeeMapper.ToDetailsDto(rawXml);

                            logsToInsert.Add(new BambooHRIntegrationLogs
                            {
                                EntityType = "time/timeout",
                                EntityActionType = "insert",
                                IntegrationStatus = "completed",
                                IntegrationDate = DateTime.UtcNow,
                                IntegrationActionDetails = $"EmployeeCode -> {mapped.EmployeeNumber}, Employee not exist in payroll system"
                            });

                            continue; // Skip to next request
                        }
                    }

                    // Prepare leave entry
                    var leave = new EmployeeLeave
                    {
                        IdEmployee = internalEmpId,
                        LeaveFromDate = req.Start,
                        LeaveToDate = req.End,
                        NoDays = req.Amount.Value,
                        AppliedDate = req.Created,
                        ApprovalStatus = req.Status.Value.ToUpper(),
                        ApprovedDate = req.Status.LastChanged,
                        IdLeaveType = req.Type.Id,
                        LeaveTypeName = req.Type.Value,
                        BambooHRRequestId = req.Id
                    };

                    leavesToInsert.Add(leave);
                }

                // Insert all leave entries
                await _dbContext.EmployeeLeaves.AddRangeAsync(leavesToInsert);
                await _dbContext.SaveChangesAsync(); // Needed to get IdEmployeeLeave

                // Build and collect leave details
                foreach (var leave in leavesToInsert)
                {
                    var requessts = requests.Requests.FirstOrDefault(r => r.Id == leave.BambooHRRequestId);
                    if (requessts?.Dates == null) continue;

                    foreach (var date in requessts.Dates)
                    {
                        leaveDetailsToInsert.Add(new EmployeeLeaveDetail
                        {
                            IdEmployeeLeave = leave.IdEmployeeLeave,
                            LeaveDate = date.Ymd,
                            AmountFlag = date.Amount > 0,
                            Amount = null
                        });
                    }
                }

                // Insert leave details
                if (leaveDetailsToInsert.Any())
                    await _dbContext.EmployeeLeaveDetails.AddRangeAsync(leaveDetailsToInsert);

                // Insert integration logs
                if (logsToInsert.Any())
                    await _dbContext.BambooHRIntegrationLogs.AddRangeAsync(logsToInsert);

                // Update last run timestamp
                var lastRun = await _dbContext.BambooHRLeaveIntegrationLastRun.FirstOrDefaultAsync();

                if (lastRun != null)
                {
                    lastRun.LeaveIntegrationLastRunDate = end;
                    _dbContext.BambooHRLeaveIntegrationLastRun.Update(lastRun);
                }
                else
                {
                    await _dbContext.BambooHRLeaveIntegrationLastRun.AddAsync(new BambooHRLeaveIntegrationLastRun
                    {
                        LeaveIntegrationLastRunDate = end
                    });
                }

                //UpdateEmployeeLeaveDetails


                var employeeLeaves = await _dbContext.EmployeeLeaves.Where(el => el.LeaveFromDate >= start && el.LeaveToDate <= end)
                    .ToListAsync();
                var unauthorizedAbsencesToUpdate = new List<EmployeeUnauthorizedAbsence>();

                // Iterate over each employee leave record
                foreach (var leave in employeeLeaves)
                {
                    // Fetch the corresponding leave details for the employee leave
                    var leaveDetails = await _dbContext.EmployeeLeaveDetails
                        .Where(ld => ld.IdEmployeeLeave == leave.IdEmployeeLeave)
                        .ToListAsync();

                    // For each leave detail, check and update unauthorized absences that match
                    foreach (var detail in leaveDetails)
                    {
                        // Fetch matching unauthorized absences for the employee on the leave date
                        var unauthorizedAbsences = await _dbContext.EmployeeUnAuthorizedAbsence
                            .Where(eua => eua.IdEmployee == leave.IdEmployee
                                          && eua.AbsentDate == detail.LeaveDate
                                          && eua.IdEmployeeLeave == null)  // Only those that don't have IdEmployeeLeave set
                            .ToListAsync();

                        // Update the unauthorized absence records
                        foreach (var abs in unauthorizedAbsences)
                        {
                            abs.IdEmployeeLeave = leave.IdEmployeeLeave;
                            abs.IdEmployeeLeaveDetails = detail.IdEmployeeLeaveDetail; // Link the EmployeeLeaveDetail
                            unauthorizedAbsencesToUpdate.Add(abs); // Collect for update
                        }
                    }
                }

                // Now update the modified EmployeeUnauthorizedAbsence records
                if (unauthorizedAbsencesToUpdate.Any())
                {
                    _dbContext.EmployeeUnAuthorizedAbsence.UpdateRange(unauthorizedAbsencesToUpdate);
                }


                // Final save and commit
                await _dbContext.SaveChangesAsync();
                await transaction.CommitAsync();

                return "Sync Completed Successfully";
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                _logger.LogError(ex, "SyncTimeOffRequests failed");
                throw new Exception("Time-off sync failed. Rolled back.");
            }
        }

        private async Task<BambooHREmployeeXmlDto> GetRawEmployeeXml(int employeeId)
        {
            var baseURL = _configuration["BambooHR:BaseUrl"];
            var apiKey = _configuration["BambooHR:ApiKey"];
            var client = new RestClient(new RestClientOptions(baseURL) { MaxTimeout = -1 });

            var detailUrl = $"/employees/{employeeId}?fields=employeenumber";
            var token = Convert.ToBase64String(Encoding.UTF8.GetBytes($"{apiKey}:x"));
            var request = new RestRequest(detailUrl, Method.Get);
            request.AddHeader("Authorization", $"Basic {token}");

            var response = await client.ExecuteAsync(request);
            if (!response.IsSuccessful)
                throw new Exception("Failed to get BambooHR employee data");

            var serializer = new XmlSerializer(typeof(BambooHREmployeeXmlDto));
            using var reader = new StringReader(response.Content);
            return serializer.Deserialize(reader) as BambooHREmployeeXmlDto;
        }


        private async Task<int> GetInternalEmployeeIdFromBambooId(int employeeId)
        {
            var baseURL = _configuration["BambooHR:BaseUrl"];
            var apiKey = _configuration["BambooHR:ApiKey"];
            var client = new RestClient(new RestClientOptions(baseURL) { MaxTimeout = -1 });

            var detailUrl = $"/employees/{employeeId}?fields=displayName,firstName,lastName,gender,dateOfBirth,address1,address2,middleName,workPhone,mobilePhone,city,state,zipcode,JoiningDate,commissionDate,supervisor,status,terminationDate,department,jobTitle,workEmail,hiredate,employeenumber,customNIS,customTIN,Exempt";

            var token = Convert.ToBase64String(Encoding.UTF8.GetBytes($"{apiKey}:x"));
            var request = new RestRequest(detailUrl, Method.Get);
            request.AddHeader("Authorization", $"Basic {token}");

            var response = await client.ExecuteAsync(request);
            if (!response.IsSuccessful)
                throw new Exception($"Failed to get details for employee ID {employeeId}. Status: {response.StatusCode}");

            var serializer = new XmlSerializer(typeof(BambooHREmployeeXmlDto));
            using var reader = new StringReader(response.Content);
            var detailedRaw = serializer.Deserialize(reader) as BambooHREmployeeXmlDto;

            if (detailedRaw == null)
                throw new Exception("Deserialization of BambooHR employee data failed.");

            var mapped = BambooEmployeeMapper.ToDetailsDto(detailedRaw);

            var employee = await _dbContext.Employees.FirstOrDefaultAsync(e => e.EmployeeCode == mapped.EmployeeNumber);
            return employee?.IdEmployee ?? throw new Exception("Employee not found in internal system");
        }
        public async Task<List<BambooHRDetailsDto>> SyncEmployeeReportingOfficerFromBambooHR()
        {
            var result = new List<BambooHRDetailsDto>();
            try
            {
                var baseURL = _configuration["BambooHR:BaseUrl"];
                var apikey = _configuration["BambooHR:ApiKey"];

                if (string.IsNullOrWhiteSpace(baseURL) || string.IsNullOrWhiteSpace(apikey))
                {
                    _logger.LogError("BambooHR BaseUrl or ApiKey is missing in configuration.");
                    return new List<BambooHRDetailsDto>();
                }

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

                var rawEmployees = directory?.Employees ?? new List<EmployeeRawDto>();

                foreach (var emp in rawEmployees)
                {
                    try
                    {
                        var detailUrl = $"{baseURL}/employees/{emp.Id}?fields=displayName,firstName,LastName,employeenumber,supervisor";

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
                        mapped.EmployeePhotoPath = emp.Fields.FirstOrDefault(f => f.Id == "photoUrl")?.Value;

                        //await UpdateEmployeeReportingOfficerFromBambooHR(mapped);
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
            var empDetails = await _dbContext.Employees
                .Where(em => em.EmployeeCode == bambooEmp.EmployeeNumber)
                .FirstOrDefaultAsync();

            IsNewEmployee = false;
            DataChanges = string.Empty;
            IsDataChangedInBambooHR = false;

            if (empDetails == null)
                IsNewEmployee = true;
            if (IsNewEmployee)
            {
                IsNewEmployee = true;
                empDetails = new Models.Employee();
            }

            empDetails.EmployeeCode= CompareStringData("EmployeeCode", empDetails.EmployeeCode,bambooEmp.EmployeeNumber.Trim());

            empDetails.FirstName = CompareStringData("FirstName", empDetails.FirstName, bambooEmp.FirstName.Trim());
            empDetails.MiddleName = CompareStringData("MiddleName", empDetails.MiddleName, bambooEmp.MiddleName?.Trim());
            empDetails.LastName = CompareStringData("LastName", empDetails.LastName, bambooEmp.LastName?.Trim());
            empDetails.Gender = CompareStringData("Gender", empDetails.Gender, bambooEmp.Gender?.Trim()).ToUpper();
            if (empDetails.Gender == "")
                empDetails.Gender = "NOTKNOWN";

            empDetails.EmailID = CompareStringData("EmailID", empDetails.EmailID, bambooEmp.WorkEmail?.Trim());
            empDetails.PhoneNumber1 = CompareStringData("PhoneNumber1", empDetails.PhoneNumber1, bambooEmp.WorkPhone?.Trim());
            empDetails.PhoneNumber2 = CompareStringData("PhoneNumber2", empDetails.PhoneNumber2, bambooEmp.MobilePhone?.Trim());
            empDetails.Address1 = CompareStringData("Address1", empDetails.Address1, bambooEmp.Address1?.Trim());
            empDetails.Address2 = CompareStringData("Address2", empDetails.Address2, bambooEmp.Address2?.Trim());
            empDetails.City = CompareStringData("City", empDetails.City, bambooEmp.City?.Trim());
            empDetails.State = CompareStringData("State", empDetails.State, bambooEmp.State?.Trim());
            empDetails.ZipCode = CompareStringData("ZipCode", empDetails.ZipCode, bambooEmp.ZipCode?.Trim());
            empDetails.IdNumber = CompareStringData("IdNumber", empDetails.IdNumber, bambooEmp.customNIS?.Trim());
            empDetails.TaxIdNumber = CompareStringData("TaxIdNumber", empDetails.TaxIdNumber, bambooEmp.customTIN?.Trim());
            empDetails.OverTimeAllowedStatus = CompareStringData("OverTimeAllowedStatus", empDetails.OverTimeAllowedStatus, bambooEmp.Exempt?.Trim());

            // Set Designation
            DesignationName = string.Empty;
            var idDesignation = await GetIdDesignation(bambooEmp.JobTitle);
            if (idDesignation > 0)
            {
                empDetails.IdDesignation = idDesignation;
                CompareStringData("Designation", DesignationName, bambooEmp.JobTitle?.Trim());
            }
            else
                DataChanges += "Designation " + bambooEmp.JobTitle + " Not found\n";

            // Set Department
            DepartmentName = string.Empty;
            var idDepartment = await GetIdDepartment(bambooEmp.Department);
            if (idDepartment > 0)
            {
                empDetails.IdDepartment = idDepartment;
                CompareStringData("Department", DepartmentName, bambooEmp.Department?.Trim());
            }
            else
                DataChanges += "Department " + bambooEmp.Department + " Not found\n";


            var reportingTo = await GetIdEmployee(bambooEmp.Supervisor);
            if (reportingTo > 0)
                empDetails.ReportingTo = reportingTo;

            empDetails.CurrentStatus = bambooEmp.Status == "Active" ? "Working" : "NotWorking";

            if (bambooEmp.TerminationDate != null)
                empDetails.LastWorkingDay = bambooEmp.TerminationDate;
            if (bambooEmp.DateOfBirth != null)
                empDetails.DateOfBirth = bambooEmp.DateOfBirth; 
            if (bambooEmp.HireDate != null)
                empDetails.JoiningDate = bambooEmp.HireDate;

            // ⬇⬇ PHOTO UPLOAD AND REPLACEMENT ⬇⬇
            if (!string.IsNullOrEmpty(bambooEmp.EmployeePhotoPath))
            {
                string folderPath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "uploads", "profileimages");
                if (!Directory.Exists(folderPath))
                    Directory.CreateDirectory(folderPath);

                // Delete old image if exists
                if (!IsNewEmployee && !string.IsNullOrEmpty(empDetails.EmployeePhotoFilePath))
                {
                    try
                    {
                        if (File.Exists(empDetails.EmployeePhotoFilePath))
                            File.Delete(empDetails.EmployeePhotoFilePath);
                    }
                    catch (Exception ex)
                    {
                        // Log but don't block the update
                        Console.WriteLine($"Failed to delete old image: {ex.Message}");
                    }
                }

                // Download and save new photo
                string fileName = $"{bambooEmp.Id}_{DateTime.Now.Ticks}.jpg";
                string filePath = Path.Combine(folderPath, fileName);

                using var httpClient = new HttpClient();
                var imageBytes = await httpClient.GetByteArrayAsync(bambooEmp.EmployeePhotoPath);
                await File.WriteAllBytesAsync(filePath, imageBytes);

                empDetails.EmployeePhotoFilePath = filePath;
            }

            // ⬇⬇ SAVE TO DB ⬇⬇
            if (!IsNewEmployee)
            {
                if (IsDataChangedInBambooHR == true)
                {
                    _dbContext.Employees.Update(empDetails);
                    await AddBambooHRIntegrationLog(empDetails.EmployeeCode, empDetails.FirstName, "UPDATE");
                }
            }
            else
            {
                _dbContext.Employees.Add(empDetails);
                DataChanges = "New Employee Added";
                await AddBambooHRIntegrationLog(empDetails.EmployeeCode, empDetails.FirstName, "NEW");
            }
            await _dbContext.SaveChangesAsync();
            if (IsNewEmployee)
            {
                var notificationConfig = await _dbContext.NotificationsConfig
                .FirstOrDefaultAsync(x => x.EntityCode == "NEWEMPLOYEE" && x.NotificationType == "New Employee Added");
                if (notificationConfig != null)
                {
                    string newEmployeeName = $"{empDetails.FirstName} {empDetails.MiddleName} {empDetails.LastName}".Replace("  ", " ").Trim();

                    string emailBody = notificationConfig.EmailContent
                        .Replace("#NEWEMPLOYEENAME#", newEmployeeName);

                    // Get employee(s) with Department = 4 and Designation = 19 or 37
                    var notifyEmployees = await _dbContext.Employees
                        .Where(e => e.IdDepartment == 4 && (e.IdDesignation == 19 || e.IdDesignation == 37))
                        .ToListAsync();

                    foreach (var approver in notifyEmployees)
                    {
                        if (!string.IsNullOrEmpty(approver.EmailID))
                        {
                            await EmailService.SendMail(
                               approver.EmailID,
                                notificationConfig.EmailSubject,
                                emailBody
                            );
                        }


                        Notification notification = new Notification();
                        notification.IdNotificationConfig = notificationConfig.IdNotificationConfig;
                        notification.NotificationType = notificationConfig.NotificationType;
                        notification.SentByIdEmployee = null;
                        notification.ReceivedByIdEmployee = approver.IdEmployee;
                        notification.EmailSubject = notificationConfig.EmailSubject;
                        notification.EmailContent = emailBody;
                        notification.EmailSentStatus = "SENT";
                        notification.CreatedAt = DateTime.UtcNow;
                         _dbContext.Notifications.Add(notification);
                    }
                    await _dbContext.SaveChangesAsync();
                }
            }

            return true;
        }
     
        public async Task<int> GetIdDesignation(string DesigName)
        {
            var desigDetails = await _dbContext.Designations.Where(dd => dd.DesignationName.ToUpper() == DesigName.ToUpper()).FirstOrDefaultAsync();
            if (desigDetails != null)
            {
                DesignationName = desigDetails.DesignationName;
                return desigDetails.IdDesignation;
            }
            return -1;
        }

        public async Task<int> GetIdDepartment(string DeptName)
        {
            var deptDetails = await _dbContext.Departments.Where(dd => dd.DepartmentName.ToUpper() == DeptName.ToUpper()).FirstOrDefaultAsync();
            if (deptDetails != null)
            {
                DepartmentName = deptDetails.DepartmentName;
                return deptDetails.IdDepartment;

            }

            return -1;
        }

        public async Task<int> GetIdEmployee(string EmployeeName)
        {
            if (EmployeeName == null)
                return -1;
            string[] splitEName = EmployeeName.Split(',');
            if (splitEName.Length == 1)
            {
                var empDetails = await _dbContext.Employees.Where(em => (em.LastName ?? "").ToUpper() == splitEName[0].Trim().ToUpper()).FirstOrDefaultAsync();
                if (empDetails != null)
                {
                    ReportingTo = empDetails.FirstName;
                    return (int)empDetails.IdEmployee;
                }
            }
            if (splitEName.Length == 2)
            {
                var empDetails = await _dbContext.Employees.Where(em => (em.LastName ?? "").ToUpper() == splitEName[0].Trim().ToUpper() && 
                em.FirstName.ToUpper() == splitEName[1].Trim().ToUpper()).FirstOrDefaultAsync();
                if (empDetails != null)
                {
                    ReportingTo = empDetails.FirstName;
                    return (int)empDetails.IdEmployee;
                }
            }
            return -1;
        }

        public string CompareStringData(string ColumnName, string FirstValue, string SecondValue)
        {
            if (!IsNewEmployee)
            {
                if (FirstValue == null)
                    FirstValue = string.Empty;
                if (SecondValue == null)
                    SecondValue = string.Empty;
                if (!string.Equals(FirstValue.ToUpper(), SecondValue.ToUpper(), StringComparison.OrdinalIgnoreCase))
                {
                    IsDataChangedInBambooHR = true;
                    DataChanges += $"{ColumnName} {FirstValue ?? ""} -> {SecondValue ?? ""}\n ";
                    FirstValue = SecondValue;
                    return FirstValue;
                    
                }
            }
            return SecondValue;
        }

        public string CompareIntegerData(string ColumnName, string FirstValue, string SecondValue)
        {
            if (!IsNewEmployee)
            {
                if (!string.Equals(FirstValue, SecondValue, StringComparison.OrdinalIgnoreCase))
                {
                    IsDataChangedInBambooHR = true;
                    DataChanges += $"{ColumnName} {FirstValue ?? ""} -> {SecondValue ?? ""}\n ";
                    FirstValue = SecondValue;
                }
            }
            return FirstValue;
        }

        public async Task<int> AddBambooHRIntegrationLog(string EmployeeCode, string EmployeeName, string ActionType)
        {

            using var transaction = await _dbContext.Database.BeginTransactionAsync();

            try
            {
                var bambooHRLog = new Models.BambooHRIntegrationLogs
                {
                    EntityType = "Employees",
                    EntityActionType = ActionType,
                    IntegrationDate = DateTime.Now,
                    IntegrationStatus = "Completed",
                    IntegrationActionDetails = DataChanges
                };
                _dbContext.BambooHRIntegrationLogs.Add(bambooHRLog);
                await _dbContext.SaveChangesAsync();          
                await transaction.CommitAsync();

                return 1;
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                _logger.LogError(ex, "Error adding, updating, or removing BambooHR Details.");
                throw new Exception("An error occurred while processing BambooHR Details. Please try again.");
            }
            return -1;
        }

        public async Task<int> UpdateEmployeeStatusNotWorkingIfNotFoundInBambooHR(List <EmployeeRawDto> bambooHREmpList)
        {

            using var transaction = await _dbContext.Database.BeginTransactionAsync();

            try
            {
                var bambooHREmailIDs = bambooHREmpList
                           .Select(x => x.Fields.FirstOrDefault(f => f.Id == "workEmail")?.Value)
                           .Where(email => !string.IsNullOrEmpty(email))
                           .ToHashSet(StringComparer.OrdinalIgnoreCase);

                var empDetails = await _dbContext.Employees.ToListAsync();
                var employeesToUpdate = empDetails
             .Where(e => !bambooHREmailIDs.Contains(e.EmailID) && e.IdEmployee > 1000)
             .ToList();
              

                        foreach (var emp in employeesToUpdate)
                        {
                            emp.CurrentStatus = "NotWorking";
                        }

                        await _dbContext.SaveChangesAsync();
                        await transaction.CommitAsync();

                        return employeesToUpdate.Count;
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                _logger.LogError(ex, "Error adding, updating, or removing BambooHR Details.");
                throw new Exception("An error occurred while processing BambooHR Details. Please try again.");
            }
        }


        public async Task<string> SyncTimeOffRequestsForLeave(DateTime start, DateTime end)
        {
            start = start.Date;
            end = end.Date;
            var bambooUsers = await GetBambooUsersAsync();
            var workYears = await _dbContext.WorkYears.ToListAsync();
            var leaveConfigQuery =
                from a in _dbContext.EmployeeLeaveConfigs
                join b in _dbContext.EmployeeLeaveConfigDetails
                    on a.IdEmployeeLeaveConfig equals b.IdEmployeeLeaveConfig
                join c in _dbContext.LeaveTemplateDetails
                    on new { b.IdLeaveType, b.IdLeaveTemplateDetail }
                    equals new { IdLeaveType = c.IdLeaveType, IdLeaveTemplateDetail = c.IdLeaveTemplateDetails }
                join d in _dbContext.LeaveTemplates
                    on c.IdLeaveTemplate equals d.IdLeaveTemplate
                where start >= a.EffectiveFrom && start <= a.EffectiveTo
                select new LeaveTemplateQueryDto
                {
                    IdEmployee = a.IdEmployee,
                    IdLeaveType = b.IdLeaveType,
                    LeaveTypeName = c.LeaveTypeName,
                    IdLeaveTemplateDetails = c.IdLeaveTemplateDetails,
                    EffectiveFrom = a.EffectiveFrom,
                    EffectiveTo = (DateTime)a.EffectiveTo,
                    LeaveTemplateName = d.LeaveTemplateName,
                    IdYear = d.IdYear
                };

            var baseUrl = "https://api.bamboohr.com";
            var apiKey = _configuration["BambooHR:ApiKey"];
            var subdomain = _configuration["BambooHR:Subdomain"];

            var client = new RestClient(new RestClientOptions(baseUrl) { MaxTimeout = -1 });
            var url = $"/api/gateway.php/{subdomain}/v1/time_off/requests/?start={start:yyyy-MM-dd}&end={end:yyyy-MM-dd}";
            var request = new RestRequest(url, Method.Get);
            var token = Convert.ToBase64String(Encoding.UTF8.GetBytes($"{apiKey}:x"));
            request.AddHeader("Authorization", $"Basic {token}");

            // Fetch BambooHR time off requests
            var response = await client.ExecuteAsync(request);
            if (!response.IsSuccessful)
                throw new Exception($"BambooHR API failed: {response.StatusCode} - {response.Content}");

            // Deserialize XML response
            var serializer = new XmlSerializer(typeof(TimeOffRequestsDto));
            using var reader = new StringReader(response.Content);
            var requests = serializer.Deserialize(reader) as TimeOffRequestsDto;

            if (requests?.Requests == null || !requests.Requests.Any())
                return "No requests found in the date range.";

            var logsToInsert = new List<BambooHRIntegrationLogs>();
            var leavesToInsert = new List<LeaveApplications>(); // For New Leave Application Insert
            var leavesToUpdate = new List<LeaveApplications>(); // For Updating Existing Records

            using var transaction = await _dbContext.Database.BeginTransactionAsync();
            try
            {
                foreach (var req in requests.Requests)
                {
                    // Check if employee exists
                    var internalEmpId = await GetInternalEmployeeIdFromBambooId(req.Employee.Id);
                    if (internalEmpId == 0) // Employee not found
                    {
                        logsToInsert.Add(new BambooHRIntegrationLogs
                        {
                            EntityType = "LEAVE",
                            EntityActionType = "INSERT",
                            IntegrationStatus = "FAILED",
                            IntegrationDate = DateTime.UtcNow,
                            IntegrationActionDetails = $"Employee {req.Employee.Id} not found in payroll system"
                        });
                        continue; // Skip processing this request
                    }

                    // Check Leave Type (ensure leave type exists)
                    var leaveType = await _dbContext.LeaveTypes.FirstOrDefaultAsync(l => l.LeaveTypeName == req.Type.Value);
                    if (leaveType == null)
                    {
                        logsToInsert.Add(new BambooHRIntegrationLogs
                        {
                            EntityType = "LEAVE",
                            EntityActionType = "INSERT",
                            IntegrationStatus = "FAILED",
                            IntegrationDate = DateTime.UtcNow,
                            IntegrationActionDetails = $"Leave Type {req.Type.Value} not found"
                        });
                        continue; // Skip processing this request
                    }

                    // Check if leave request with the same BambooHRRequestId exists
                    var existingLeaveApplication = await _dbContext.LeaveApplications
                        .FirstOrDefaultAsync(l => l.BambooHRLeaveRequestID == req.Id && l.IdEmployee == internalEmpId);

                    if (existingLeaveApplication != null)
                    {
                        // Update existing leave application
                        existingLeaveApplication.FromDate = req.Start;
                        existingLeaveApplication.ToDate = req.End;
                        existingLeaveApplication.TotalLeaveDays = req.Amount.Value;
                        existingLeaveApplication.AppliedOn = req.Created;
                        existingLeaveApplication.Reason = req.Notes.Notes.FirstOrDefault(n => n.From == "employee")?.Value;
                        existingLeaveApplication.ApprovalStatus = req.Status.Value.ToUpper();
                        existingLeaveApplication.CancelledDate = req.Status.Value.ToUpper() == "canceled" ? req.Status.LastChanged : (DateTime?)null;
                        existingLeaveApplication.ReasonForCancellation = req.Status.Value.ToUpper() == "canceled"
                            ? req.Notes.Notes.FirstOrDefault(n => n.From == "manager")?.Value
                            : null;

                        // Add to update list (don't set IdLeaveApplication, it is used by EF for update)
                        leavesToUpdate.Add(existingLeaveApplication);

                        logsToInsert.Add(new BambooHRIntegrationLogs
                        {
                            EntityType = "LEAVE",
                            EntityActionType = "UPDATE",
                            IntegrationStatus = "COMPLETED",
                            IntegrationDate = DateTime.UtcNow,
                            IntegrationActionDetails = $"Updated leave application for employee {internalEmpId}, request ID: {req.Id}"
                        });
                    }
                    else
                    {
                        // Check for date overlap
                        var overlappingLeave = await _dbContext.LeaveApplications
                        .Where(l => l.IdEmployee == internalEmpId &&
                                    ((l.FromDate <= req.End && l.ToDate >= req.Start) ||
                                     (l.FromDate >= req.Start && l.ToDate <= req.End)))
                        .FirstOrDefaultAsync();

                        var leaveTemplate = await leaveConfigQuery
                            .FirstOrDefaultAsync(l => l.LeaveTypeName == req.Type.Value);

                        if (leaveTemplate == null || leaveTemplate.IdLeaveTemplateDetails == 0)
                        {
                            logsToInsert.Add(new BambooHRIntegrationLogs
                            {
                                EntityType = "LEAVE",
                                EntityActionType = "INSERT",
                                IntegrationStatus = "FAILED",
                                IntegrationDate = DateTime.UtcNow,
                                IntegrationActionDetails = $"Leave template details not found for request ID: {req.Id}, employee {internalEmpId}"
                            });
                            continue; // Skip processing this request
                        }

                        if (overlappingLeave != null)
                        {
                            logsToInsert.Add(new BambooHRIntegrationLogs
                            {
                                EntityType = "LEAVE",
                                EntityActionType = "INSERT",
                                IntegrationStatus = "FAILED",
                                IntegrationDate = DateTime.UtcNow,
                                IntegrationActionDetails = $"Leave request overlaps with an existing leave for employee {internalEmpId}, request ID: {req.Id}"
                            });
                            continue; // Skip processing this request
                        }

                        var workYear = workYears
                            .Where(w => req.Start >= w.WorkDateFrom && req.End <= w.WorkDateTo)
                            .ToList();

                        if (workYear.Count != 1)
                        {
                            logsToInsert.Add(new BambooHRIntegrationLogs
                            {
                                EntityType = "LEAVE",
                                EntityActionType = "INSERT",
                                IntegrationStatus = "FAILED",
                                IntegrationDate = DateTime.UtcNow,
                                IntegrationActionDetails = $"Leave request spans across multiple work years or does not match any work year for employee {internalEmpId}, request ID: {req.Id}"
                            });
                            continue; // Skip processing this request
                        }
                        var approvedByInternalId = 0;

                        if (!string.IsNullOrEmpty(req.Status.LastChangedByUserId))
                        {
                            if (bambooUsers.TryGetValue(req.Status.LastChangedByUserId, out var bambooEmployeeId))
                            {
                                approvedByInternalId = await GetInternalEmployeeIdFromBambooId(Convert.ToInt32(bambooEmployeeId));
                            }
                        }


                        if (approvedByInternalId == 0)
                        {
                            logsToInsert.Add(new BambooHRIntegrationLogs
                            {
                                EntityType = "LEAVE",
                                EntityActionType = "APPROVER_CHECK",
                                IntegrationStatus = "FAILED",
                                IntegrationDate = DateTime.UtcNow,
                                IntegrationActionDetails = $"Approver not found for Bamboo user {req.Status.LastChangedByUserId}"
                            });
                            continue; // Skip processing this request
                        }


                        // Prepare the leave application for insertion
                        var leaveApplication = new LeaveApplications
                        {
                            IdEmployee = internalEmpId,
                            IdLeaveType = leaveType.IdLeaveType,
                            FromDate = req.Start,
                            ToDate = req.End,
                            TotalLeaveDays = req.Amount.Value,
                            AppliedOn = req.Created,
                            Reason = req.Notes.Notes.FirstOrDefault(n => n.From == "employee")?.Value,
                            ApprovalStatus = req.Status.Value.ToUpper(),
                            BambooHRLeaveRequestID = req.Id,
                            LeaveTypeName = leaveTemplate.LeaveTemplateName,
                            IdYear = workYear.FirstOrDefault().IdWorkYear,
                            ApprovedBy = approvedByInternalId,
                            IdLeaveTemplateDetail = leaveTemplate.IdLeaveTemplateDetails,
                            CancelledDate = req.Status.Value == "canceled" ? req.Status.LastChanged : (DateTime?)null,
                            ReasonForCancellation = req.Status.Value.ToUpper() == "canceled"
                                ? req.Notes.Notes.FirstOrDefault(n => n.From == "manager")?.Value
                                : null
                        };

                        leavesToInsert.Add(leaveApplication);

                        logsToInsert.Add(new BambooHRIntegrationLogs
                        {
                            EntityType = "LEAVE",
                            EntityActionType = "INSERT",
                            IntegrationStatus = "COMPLETED",
                            IntegrationDate = DateTime.UtcNow,
                            IntegrationActionDetails = $"Inserted leave application for employee {internalEmpId}, request ID: {req.Id}"
                        });
                    }
                }

                // Insert new records (leavesToInsert)
                if (leavesToInsert.Any())
                    await _dbContext.LeaveApplications.AddRangeAsync(leavesToInsert);

                // Update existing records (leavesToUpdate)
                if (leavesToUpdate.Any())
                    _dbContext.LeaveApplications.UpdateRange(leavesToUpdate);

                // Insert BambooHR integration logs
                if (logsToInsert.Any())
                    await _dbContext.BambooHRIntegrationLogs.AddRangeAsync(logsToInsert);

                // Commit the transaction
                await _dbContext.SaveChangesAsync();
                await transaction.CommitAsync();
                var lastRun = await _dbContext.BambooHRLeaveIntegrationLastRun.FirstOrDefaultAsync();

                if (lastRun != null)
                {
                    lastRun.LeaveIntegrationLastRunDate = DateTime.UtcNow.Date;
                    _dbContext.BambooHRLeaveIntegrationLastRun.Update(lastRun);
                }
                else
                {
                    await _dbContext.BambooHRLeaveIntegrationLastRun.AddAsync(
                        new BambooHRLeaveIntegrationLastRun
                        {
                            LeaveIntegrationLastRunDate = DateTime.UtcNow.Date
                        });
                }

                await _dbContext.SaveChangesAsync();

                return "Sync Completed Successfully";
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                _logger.LogError(ex, "SyncTimeOffRequests failed for leave");
                throw new Exception("Time-off sync failed. Rolled back.");
            }
        }
        private async Task<Dictionary<string, string>> GetBambooUsersAsync()
        {
            var baseUrl = "https://api.bamboohr.com";
            var apiKey = _configuration["BambooHR:ApiKey"];
            var subdomain = _configuration["BambooHR:Subdomain"];

            var client = new RestClient(new RestClientOptions(baseUrl));
            var request = new RestRequest($"/api/gateway.php/{subdomain}/v1/meta/users", Method.Get);

            var token = Convert.ToBase64String(Encoding.UTF8.GetBytes($"{apiKey}:x"));
            request.AddHeader("Authorization", $"Basic {token}");

            var response = await client.ExecuteAsync(request);

            if (!response.IsSuccessful)
                throw new Exception("Failed to fetch Bamboo users");

            var serializer = new XmlSerializer(typeof(BambooUsersDto));
            using var reader = new StringReader(response.Content);
            var result = serializer.Deserialize(reader) as BambooUsersDto;

            // Dictionary<UserId, EmployeeId>
            return result.Users.ToDictionary(u => u.Id, u => u.EmployeeId);
        }
    }



}
