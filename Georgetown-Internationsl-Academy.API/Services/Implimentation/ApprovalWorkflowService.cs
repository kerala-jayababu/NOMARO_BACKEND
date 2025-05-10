using Dapper;
using Georgetown_International_Academy.API.Database;
using Georgetown_Internationsl_Academy.API.DTO;
using Georgetown_Internationsl_Academy.API.Helpers;
using Georgetown_Internationsl_Academy.API.Models;
using Georgetown_Internationsl_Academy.API.Services.Interface;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json.Linq;
using Org.BouncyCastle.Ocsp;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Georgetown_Internationsl_Academy.API.Services.Implimentation
{
    public class ApprovalWorkflowService : IApprovalWorkflowService
    {
        private readonly ApplicationDBContext _dbContext;
        private readonly IConfiguration _configuration;
        private readonly IAccountService _accountService;
        private readonly IHttpContextAccessor _httpContextAccessor;
        public ApprovalWorkflowService(ApplicationDBContext dbContext, IConfiguration configuration, IHttpContextAccessor httpContextAccessor, IAccountService accountService)
        {
            _dbContext = dbContext;
            _configuration = configuration;
            _accountService = accountService;
            _httpContextAccessor = httpContextAccessor;
        }

        public async Task<string> InitiateApprovalWorkflow(int entityTablePrimaryKeyID, string entityCode, int loggedInEmployeeId, string? status, string? rejectReason)
        {


            try
            {
                var currentRecord = await _dbContext.ApprovalWorkFlowAllocations
                             .Where(a => a.EntityTablePrimaryKeyID == entityTablePrimaryKeyID && a.EntityCode == entityCode)
                             .OrderByDescending(a => a.LevelNumber)
                             .ThenByDescending(a => a.CycleIndex)
                             .FirstOrDefaultAsync();

                if (currentRecord == null || status == "SUBMITTED")
                {
                    // Step 1: Start the approval workflow by creating the first level
                    var workflowConfig = await _dbContext.WorkFlowConfig
                        .FirstOrDefaultAsync(w => w.EntityCode == entityCode);

                    if (workflowConfig == null)
                    {
                        return "Workflow configuration not found for the provided EntityCode.";
                    }

                    var firstLevel = await _dbContext.WorkFlowConfigDetails
                        .FirstOrDefaultAsync(w => w.IdWorkFlowConfig == workflowConfig.IdWorkFlowConfig && w.LevelNumber == 1);

                    if (firstLevel == null)
                    {
                        return "Workflow configuration details for Level 1 not found.";
                    }

                    // Step 2: Determine TargetIdEmployee
                    var targetEmployees = await GetTargetEmployees(firstLevel);

                    if (!targetEmployees.Any())
                    {
                        return "No target employees found for the first level.";
                    }

                    var targetEmployeeIds = string.Join(",", targetEmployees);
                    string actionStatussubmitted = (targetEmployeeIds == "0") ? "FINAL APPROVED" : "SUBMITTED";

                    var newRecord = new ApprovalWorkFlowAllocation
                    {
                        IdWorkFlowConfig = workflowConfig.IdWorkFlowConfig,
                        EntityCode = entityCode,
                        EntityTablePrimaryKeyID = entityTablePrimaryKeyID,
                        CycleIndex = (currentRecord != null) ? currentRecord.CycleIndex + 1 : 1,
                        LevelNumber = 1,
                        SourceIdEmployee = loggedInEmployeeId,
                        TargetIdEmployee = targetEmployeeIds,
                        ActionStatus = actionStatussubmitted,
                        SentDate = DateTime.Now
                    };

                    await _dbContext.ApprovalWorkFlowAllocations.AddAsync(newRecord);
                    await _dbContext.SaveChangesAsync();
                    if (actionStatussubmitted == "FINAL APPROVED")
                    {
                        await UpdateEntityStatus(entityTablePrimaryKeyID, entityCode, "FINAL APPROVED", 1, null,null);                        
                        return "Approval workflow initiated.";
                    }
                    else
                    {
                        await UpdateEntityStatus(entityTablePrimaryKeyID, entityCode, "SUBMITTED", 1, loggedInEmployeeId, targetEmployeeIds);
                    }

                    return "Approval workflow initiated.";
                }

                // Step 3: Validate logged-in employee
                if (currentRecord.TargetIdEmployee == null ||
                    !currentRecord.TargetIdEmployee.Split(',').Contains(loggedInEmployeeId.ToString()))
                {
                    return "Error:You are not authorized to approve/reject this record.";
                }






                // Step 4: Update current level's status
                currentRecord.ActionedBy = loggedInEmployeeId;
                currentRecord.ActionStatus = status;
                currentRecord.ActionDate = DateTime.Now;
                currentRecord.RejectionRemarks = status == "REJECTED" ? rejectReason : null;
                await _dbContext.SaveChangesAsync();

                // Step 5: Handle rejection
                if (status == "REJECTED")
                {
                   
                    var workflowConfigDetailsFinal = await _dbContext.WorkFlowConfigDetails
                        .FirstOrDefaultAsync(w => w.IdWorkFlowConfig == currentRecord.IdWorkFlowConfig && w.LevelNumber == currentRecord.LevelNumber);

                    await UpdateEntityStatus(entityTablePrimaryKeyID, entityCode, workflowConfigDetailsFinal.ApprovalStatusName, currentRecord.CycleIndex, loggedInEmployeeId, null);
                    //await transaction.CommitAsync();
                    return "Record rejected successfully. Workflow terminated.";
                }

                // Step 6: Increment level and check for the next level
                var nextLevelNumber = currentRecord.LevelNumber + 1;
                var workflowConfigDetails = await _dbContext.WorkFlowConfigDetails
                    .FirstOrDefaultAsync(w => w.IdWorkFlowConfig == currentRecord.IdWorkFlowConfig && w.LevelNumber == nextLevelNumber);

                if (workflowConfigDetails == null)
                {
                    //await transaction.CommitAsync();
                    await UpdateEntityStatus(entityTablePrimaryKeyID, entityCode, workflowConfigDetails.ApprovalStatusName, currentRecord.CycleIndex, loggedInEmployeeId,null);
                    return "Record approved successfully. Workflow completed.";
                }

                // Step 7: Insert the next level record
                var targetEmployeesForNextLevel = await GetTargetEmployees(workflowConfigDetails);

                if (!targetEmployeesForNextLevel.Any())
                {
                    return "No target employees found for the next level.";
                }

                var targetEmployeeIdsForNextLevel = string.Join(",", targetEmployeesForNextLevel);
                string actionStatus = (targetEmployeeIdsForNextLevel == "0") ? "FINAL APPROVED" : workflowConfigDetails.ApprovalStatusName;
                var newNextLevelRecord = new ApprovalWorkFlowAllocation
                {
                    IdWorkFlowConfig = currentRecord.IdWorkFlowConfig,
                    EntityCode = currentRecord.EntityCode,
                    EntityTablePrimaryKeyID = currentRecord.EntityTablePrimaryKeyID,
                    CycleIndex = currentRecord.CycleIndex,
                    LevelNumber = nextLevelNumber,
                    ActionStatus= actionStatus,
                    SourceIdEmployee = loggedInEmployeeId,
                    TargetIdEmployee = targetEmployeeIdsForNextLevel,                    
                    SentDate = DateTime.Now
                };

                await _dbContext.ApprovalWorkFlowAllocations.AddAsync(newNextLevelRecord);
                await UpdateEntityStatus(entityTablePrimaryKeyID, entityCode, actionStatus, newNextLevelRecord.CycleIndex, loggedInEmployeeId,targetEmployeeIdsForNextLevel);
                await _dbContext.SaveChangesAsync();
                //await transaction.CommitAsync();

                return "Record approved and moved to the next level.";
            }
            catch (Exception ex)
            {
                //await transaction.RollbackAsync();
                return $"Error processing approval workflow: {ex.Message}";
            }
        }




        private async Task UpdateEntityStatus(int entityTablePrimaryKeyID, string entityCode, string finalStatus, int cycleIndex, int? loggedInEmployeeId, string targetEmployeeIdsForNextLevel)
        {
            if (entityCode == _configuration["WorkflowEntityCodes:SalaryTemplate"])
            {
                var entity = await _dbContext.SalaryTemplates.FindAsync(entityTablePrimaryKeyID);
                if (entity != null)
                {
                    entity.ApprovalStatus = finalStatus;
                    await _dbContext.SaveChangesAsync();
                }

                if (finalStatus == "SUBMITTED")
                {
                    var employeeIdList = ParseEmployeeIds(targetEmployeeIdsForNextLevel);


                    foreach (var empId in employeeIdList)
                    {

                        var approverName = await GetFullNameById(empId);
                        var creatorName = await GetFullNameById(loggedInEmployeeId);
                        string createdDateTime = DateTime.Now.ToString("dd-MMM-yyyy hh:mm tt");

                        var notificationConfig = await _dbContext.NotificationsConfig
                            .FirstOrDefaultAsync(x => x.EntityCode == "SALTEM" && x.NotificationType == "Salary Template Submitted for Approval");                    

                        var notification = await CreateNotificationforSalaryTemplate(empId, (int)loggedInEmployeeId, notificationConfig, approverName.FullName, creatorName.FullName, entityTablePrimaryKeyID, createdDateTime,entity.SalaryTemplateName, "SUBMITTED",null);
                        await _dbContext.Notifications.AddAsync(notification);
                        await _dbContext.SaveChangesAsync();

                        var tokenDetails = await _accountService.LoginForMail(empId);
                        if (tokenDetails != null)
                        {
                            var tokenvalue = $"{tokenDetails.Token},{notification.IdNotification}";
                            string actionUrl = GenerateActionUrl(tokenvalue);
                            string emailBody = await GenerateEmailBody(notificationConfig.EmailContent, empId, loggedInEmployeeId, entity.SalaryTemplateName, actionUrl);

                            await EmailService.SendMail(approverName.Email, notificationConfig.EmailSubject, emailBody);
                        }
                    }
                }
                else if (finalStatus == "APPROVED" || finalStatus == "REJECTED")
                {
                    string notifType = finalStatus == "APPROVED"
                        ? "Salary Template Approved"
                        : "Salary Template Rejected";


                    var notificationConfig = await _dbContext.NotificationsConfig
                        .FirstOrDefaultAsync(x => x.EntityCode == "SALTEM" && x.NotificationType == notifType);

                    var approvalworkflow = await _dbContext.ApprovalWorkFlowAllocations
                        .FirstOrDefaultAsync(x => x.EntityTablePrimaryKeyID == entityTablePrimaryKeyID &&
                                                  x.EntityCode == "SALTEM" &&
                          
                                                  x.CycleIndex == cycleIndex);
                    var approverName = await GetFullNameById(loggedInEmployeeId);
                    var creatorName = await GetFullNameById(approvalworkflow.SourceIdEmployee);
                    string approvedDateTime = DateTime.Now.ToString("dd-MMM-yyyy hh:mm tt");
                    if (approvalworkflow != null)
                    {
                        var notification = await CreateNotificationforSalaryTemplate((int)loggedInEmployeeId, approvalworkflow.SourceIdEmployee,  notificationConfig, approverName.FullName, creatorName.FullName, approvalworkflow.EntityTablePrimaryKeyID, approvedDateTime, entity.SalaryTemplateName, finalStatus, approvalworkflow.RejectionRemarks);

                        await _dbContext.Notifications.AddAsync(notification);
                        await _dbContext.SaveChangesAsync();

                        var tokenDetails = await _accountService.LoginForMail(approvalworkflow.SourceIdEmployee);
                        if (tokenDetails != null)
                        {
                            var tokenvalue = $"{tokenDetails.Token},{notification.IdNotification}";
                            string actionUrl = GenerateActionUrl(tokenvalue);

                            var entitys = await _dbContext.SalaryTemplates.FindAsync(entityTablePrimaryKeyID);
                           
                            string salaryTemplateName = entitys?.SalaryTemplateName ?? "N/A";
          

                            string emailBody = "";

                            if (finalStatus == "APPROVED")
                            {
                                emailBody = notificationConfig.EmailContent
                                    .Replace("#CREATORNAME#", creatorName.FullName)
                                    .Replace("#SALARYTEMPLATENAME#", salaryTemplateName)
                                    .Replace("#APPROVERNAME#", approverName.FullName)
                                    .Replace("#APPROVEDDATETIME#", approvedDateTime);
                            }
                            else // REJECTED
                            {
                                emailBody = notificationConfig.EmailContent
                                    .Replace("#CREATORNAME#", creatorName.FullName)
                                    .Replace("#SALARYTEMPLATENAME#", salaryTemplateName)
                                    .Replace("#APPROVERNAME#", approverName.FullName)
                                    .Replace("#REJECTIONREASON#", approvalworkflow.RejectionRemarks ?? "No reason provided.");
                            }

                            emailBody += $"<p><a href='{actionUrl}'>Click here to view the Salary Template</a></p>";

                            await EmailService.SendMail(creatorName.Email, notificationConfig.EmailSubject, emailBody);
                        }
                    }
                }

            }
            else if (entityCode == _configuration["WorkflowEntityCodes:EmployeeSalaryConfig"])
            {
                var entity = await _dbContext.EmployeeSalaryConfig.FindAsync(entityTablePrimaryKeyID);
                var employeedetails = await _dbContext.Employees.Where(x => x.IdEmployee == entity.IdEmployee).FirstOrDefaultAsync();
                var employeename = string.Concat(employeedetails.FirstName, employeedetails.MiddleName, employeedetails.LastName);
                if (entity != null)
                {
                    entity.ApprovalStatus = finalStatus;
                    await _dbContext.SaveChangesAsync();
                }

                if (finalStatus == "SUBMITTED")
                {
                    var employeeIdList = ParseEmployeeIds(targetEmployeeIdsForNextLevel);


                    foreach (var empId in employeeIdList)
                    {

                        var approverName = await GetFullNameById(empId);
                        var creatorName = await GetFullNameById(loggedInEmployeeId);
                        string createdDateTime = DateTime.Now.ToString("dd-MMM-yyyy hh:mm tt");

                        var notificationConfig = await _dbContext.NotificationsConfig
                            .FirstOrDefaultAsync(x => x.EntityCode == "SALTEM" && x.NotificationType == "Employee Salary Config Submitted for Approval");

                        var notification = await CreateNotificationforEnmployeeSalaryConfig(approverName.FullName, employeename, creatorName.FullName, createdDateTime, notificationConfig, (int)loggedInEmployeeId, empId, entityTablePrimaryKeyID, "SUBMITTED", null);
                        await _dbContext.Notifications.AddAsync(notification);
                        await _dbContext.SaveChangesAsync();

                        var tokenDetails = await _accountService.LoginForMail(empId);
                        if (tokenDetails != null)
                        {
                            var tokenvalue = $"{tokenDetails.Token},{notification.IdNotification}";
                            string actionUrl = GenerateActionUrlForEmployeeSalaryConfig(tokenvalue);
                            string emailBody = await GenerateEmailBodyForEmployeeSalryConfig(notificationConfig.EmailContent, empId, loggedInEmployeeId, employeename, actionUrl);

                            await EmailService.SendMail(approverName.Email, notificationConfig.EmailSubject, emailBody);
                        }
                    }
                }
                else if (finalStatus == "APPROVED" || finalStatus == "REJECTED")
                {
                    string notifType = finalStatus == "APPROVED"
                        ? "Employee Salary Config Approved"
                        : "Employee Salary Config  Rejected";


                    var notificationConfig = await _dbContext.NotificationsConfig
                        .FirstOrDefaultAsync(x => x.EntityCode == "SALTEM" && x.NotificationType == notifType);

                    var approvalworkflow = await _dbContext.ApprovalWorkFlowAllocations
                        .FirstOrDefaultAsync(x => x.EntityTablePrimaryKeyID == entityTablePrimaryKeyID &&
                                                  x.EntityCode == "EMPSALCONFIG" &&
                                                  x.CycleIndex == cycleIndex);
                    var approverName = await GetFullNameById(loggedInEmployeeId);
                    var creatorName = await GetFullNameById(approvalworkflow.SourceIdEmployee);
                    string approvedDateTime = DateTime.Now.ToString("dd-MMM-yyyy hh:mm tt");
                    if (approvalworkflow != null)
                    {
                        var notification = await CreateNotificationforEnmployeeSalaryConfig(approverName.FullName, employeename, creatorName.FullName, approvedDateTime, notificationConfig, (int)loggedInEmployeeId, approvalworkflow.SourceIdEmployee, entityTablePrimaryKeyID, finalStatus, approvalworkflow.RejectionRemarks); 

                        await _dbContext.Notifications.AddAsync(notification);
                        await _dbContext.SaveChangesAsync();

                        var tokenDetails = await _accountService.LoginForMail(approvalworkflow.SourceIdEmployee);
                        if (tokenDetails != null)
                        {
                            var tokenvalue = $"{tokenDetails.Token},{notification.IdNotification}";
                            string actionUrl = GenerateActionUrlForEmployeeSalaryConfigForAPPROVEDREjected(tokenvalue);

                            var entitys = await _dbContext.SalaryTemplates.FindAsync(entityTablePrimaryKeyID);

                            string salaryTemplateName = entitys?.SalaryTemplateName ?? "N/A";


                            string emailBody = "";

                            if (finalStatus == "APPROVED")
                            {
                                emailBody = notificationConfig.EmailContent
                                    .Replace("#CREATERNAME#", creatorName.FullName)
                                    .Replace("#SALARYEMPLOYEENAME#", employeename)
                                    .Replace("#APPROVERNAME#", approverName.FullName)
                                    .Replace("#APPROVEDDATETIME#", approvedDateTime);
                            }
                            else // REJECTED
                            {
                                emailBody = notificationConfig.EmailContent
                                    .Replace("##RECEIVEDEMPLOYEENAME##", creatorName.FullName)
                                    .Replace("#SALARYEMPLOYEENAME#", employeename)
                                    .Replace("#CREATERNAME#", approverName.FullName)
                                    .Replace("#APPROVEDDATETIME#", approvedDateTime)
                                    .Replace("#REJECTIONREASON#", approvalworkflow.RejectionRemarks ?? "No reason provided.");
                            }
                            emailBody += $"<p><a href='{actionUrl}'>Click here to view the employee salry config</a></p>";
                            await EmailService.SendMail(creatorName.Email, notificationConfig.EmailSubject, emailBody);
                        }
                    }
                }



                }
            else if (entityCode == _configuration["WorkflowEntityCodes:OVERTIME"])
            {
                var entity = await _dbContext.OvertimeTransactions.FindAsync(entityTablePrimaryKeyID);
                var employeedetails = await _dbContext.Employees.Where(x => x.IdEmployee == entity.IdEmployee).FirstOrDefaultAsync();
                var employeename = string.Concat(employeedetails.FirstName, employeedetails.MiddleName, employeedetails.LastName);
                if (entity != null)
                {
                    entity.ApprovalStatus = finalStatus;
                    await _dbContext.SaveChangesAsync();
                }


                if (finalStatus == "SUBMITTED")
                {
                    var employeeIdList = ParseEmployeeIds(targetEmployeeIdsForNextLevel);


                    foreach (var empId in employeeIdList)
                    {

                        var approverName = await GetFullNameById(empId);
                        var creatorName = await GetFullNameById(loggedInEmployeeId);
                        string createdDateTime = DateTime.Now.ToString("dd-MMM-yyyy hh:mm tt");

                        var notificationConfig = await _dbContext.NotificationsConfig
                            .FirstOrDefaultAsync(x => x.EntityCode == "OVERTIME" && x.NotificationType == "Overtime Transaction Submitted for Approval");

                        var notification = await CreateNotificationforOvertimeConfig(approverName.FullName, employeename, creatorName.FullName, createdDateTime, notificationConfig, (int)loggedInEmployeeId, empId, entityTablePrimaryKeyID, "SUBMITTED", null);
                        await _dbContext.Notifications.AddAsync(notification);
                        await _dbContext.SaveChangesAsync();

                        var tokenDetails = await _accountService.LoginForMail(empId);
                        if (tokenDetails != null)
                        {
                            var tokenvalue = $"{tokenDetails.Token},{notification.IdNotification}";
                            string actionUrl = GenerateActionUrlForEmployeeSalaryConfig(tokenvalue);
                            string emailBody = await GenerateEmailBodyForOvertimeConfig(notificationConfig.EmailContent, empId, loggedInEmployeeId, employeename, actionUrl);

                            await EmailService.SendMail(approverName.Email, notificationConfig.EmailSubject, emailBody);
                        }
                    }
                }
                else if (finalStatus == "APPROVED" || finalStatus == "REJECTED" || finalStatus == "INTERIM APPROVED")
                {
                    string notifType = string.Empty;

                    if (finalStatus == "INTERIM APPROVED")
                        notifType = "Overtime Transaction Interim Approved";
                    else if (finalStatus == "APPROVED")
                        notifType = "Overtime Transaction Approved";
                    else
                        notifType = "Overtime Transaction Rejected";


                    var notificationConfig = await _dbContext.NotificationsConfig
                        .FirstOrDefaultAsync(x => x.EntityCode == "OVERTIME" && x.NotificationType == notifType);

                    var approvalworkflow = await _dbContext.ApprovalWorkFlowAllocations
                        .FirstOrDefaultAsync(x => x.EntityTablePrimaryKeyID == entityTablePrimaryKeyID &&
                                                  x.EntityCode == "OVERTIME" &&
                                                  x.CycleIndex == cycleIndex);
                    var approverName = await GetFullNameById(loggedInEmployeeId);
                    var creatorName = await GetFullNameById(approvalworkflow.SourceIdEmployee);
                    string approvedDateTime = DateTime.Now.ToString("dd-MMM-yyyy hh:mm tt");
                    if (approvalworkflow != null)
                    {
                        var notification = await CreateNotificationforOvertimeConfig(approverName.FullName, employeename, creatorName.FullName, approvedDateTime, notificationConfig, (int)loggedInEmployeeId, approvalworkflow.SourceIdEmployee, entityTablePrimaryKeyID, finalStatus, approvalworkflow.RejectionRemarks);

                        await _dbContext.Notifications.AddAsync(notification);
                        await _dbContext.SaveChangesAsync();

                        var tokenDetails = await _accountService.LoginForMail(approvalworkflow.SourceIdEmployee);
                        if (tokenDetails != null)
                        {
                            var tokenvalue = $"{tokenDetails.Token},{notification.IdNotification}";
                            string actionUrl = GenerateActionUrlForOvertimeConfigForAPPROVEDREjected(tokenvalue);

                            var entitys = await _dbContext.SalaryTemplates.FindAsync(entityTablePrimaryKeyID);

                            string salaryTemplateName = entitys?.SalaryTemplateName ?? "N/A";


                            string emailBody = "";
                            if (finalStatus == "INTERIM APPROVED")
                            {
                                emailBody = notificationConfig.EmailContent
                                    .Replace("#CREATORNAME#", creatorName.FullName)
                                    .Replace("#APPROVERNAME#", approverName.FullName)
                                    .Replace("#APPROVEDEDDATETIME#", approvedDateTime);
                            }else
                            if (finalStatus == "APPROVED")
                            {
                                emailBody = notificationConfig.EmailContent
                                    .Replace("#CREATORNAME#", creatorName.FullName)                                
                                    .Replace("#APPROVEDEDDATETIME#", approvedDateTime);
                            }
                            else 
                            {
                                emailBody = notificationConfig.EmailContent
                                    .Replace("#RECEIVEDEMPLOYEENAME#", creatorName.FullName)
                                    .Replace("#REJECTEDEMPLOYEENAME#", approverName.FullName)                                    
                                    .Replace("#REJECTEDEDDATETIME#", approvedDateTime)
                                    .Replace("#REJECTIONREASON#", approvalworkflow.RejectionRemarks ?? "No reason provided.");
                            }
                            emailBody += $"<p><a href='{actionUrl}'>Click here to view the Overtime Tranasaction</a></p>";
                            await EmailService.SendMail(creatorName.Email, notificationConfig.EmailSubject, emailBody);
                        }
                    }
                }




            }
            else if (entityCode == _configuration["WorkflowEntityCodes:EMPSALGEN"])
            {
                var entity = await _dbContext.EmployeeSalaries.FindAsync(entityTablePrimaryKeyID);
                if (entity != null)
                {
                    entity.ApprovalStatus = finalStatus;
                    entity.ApprovedDate = DateTime.Now;
                    entity.IdApprovedBy = loggedInEmployeeId;
                    await _dbContext.SaveChangesAsync();
                }
            }
        }



        private List<int> ParseEmployeeIds(string csv)
        {
            return csv?.Split(',', StringSplitOptions.RemoveEmptyEntries)
                       .Select(id => int.TryParse(id.Trim(), out var val) ? val : (int?)null)
                       .Where(id => id.HasValue)
                       .Select(id => id.Value)
                       .ToList() ?? new List<int>();
        }

        private string GenerateActionUrl(string token)
        {
            var baseUrl = _configuration["BaseURL"];          
            return $"{baseUrl}/#/auth/salary-templates?tk={token}";
        }


        private string GenerateActionUrlForEmployeeSalaryConfig(string token)
        {
            var baseUrl = _configuration["BaseURL"];
            return $"{baseUrl}/#/auth/config-approvals?tk={token}";
        }

        private string GenerateActionUrlForEmployeeSalaryConfigForAPPROVEDREjected(string token)
        {
            var baseUrl = _configuration["BaseURL"];
            return $"{baseUrl}/#/auth/employee-salary-config?tk={token}";
        }

        private string GenerateActionUrlForOvertimeConfigForAPPROVEDREjected(string token)
        {
            var baseUrl = _configuration["BaseURL"];
            return $"{baseUrl}/#/auth/overtime-transactions?tk={token}";
        }
        private async Task<string> GenerateEmailBody(string template, int approverId, int? creatorId, string salaryTemplateName, string actionUrl)
        {
            var approverName = await GetFullNameById(approverId);
            var creatorName = await GetFullNameById(creatorId);
            string createdDateTime = DateTime.Now.ToString("dd-MMM-yyyy hh:mm tt");

            string content = template
                .Replace("#APPROVERNAME#", approverName.FullName)
                .Replace("#SALARYTEMPLATENAME#", salaryTemplateName)
                .Replace("#CREATORNAME#", creatorName.FullName)
                .Replace("#CREATEDDATETIME#", createdDateTime);

            content += $"<p><a href='{actionUrl}'>Click here to open the Salary Template</a></p>";
            return content;
        }

        private async Task<string> GenerateEmailBodyForEmployeeSalryConfig(string template, int approverId, int? creatorId,string EnmployeeName, string actionUrl)
        {
            var approverName = await GetFullNameById(approverId);
            var creatorName = await GetFullNameById(creatorId);
            string createdDateTime = DateTime.Now.ToString("dd-MMM-yyyy hh:mm tt");

            string content = template
                .Replace("#APPROVERNAME#", approverName.FullName)
                .Replace("#SALARYEMPLOYEENAME#", EnmployeeName)
                .Replace("#CREATORNAME#", creatorName.FullName)
                .Replace("#CREATEDDATETIME#", createdDateTime);

            content += $"<p><a href='{actionUrl}'>Click here to open the config Approval</a></p>";
            return content;
        }

        private async Task<string> GenerateEmailBodyForOvertimeConfig(string template, int approverId, int? creatorId, string EnmployeeName, string actionUrl)
        {
            var approverName = await GetFullNameById(approverId);
            var creatorName = await GetFullNameById(creatorId);
            string createdDateTime = DateTime.Now.ToString("dd-MMM-yyyy hh:mm tt");

            string content = template
                .Replace("#APPROVERNAME#", approverName.FullName)             
                .Replace("#CREATORNAME#", creatorName.FullName)
                .Replace("#CREATEDDATETIME#", createdDateTime);

            content += $"<p><a href='{actionUrl}'>Click here to open the config Approval</a></p>";
            return content;
        }


        private async Task<(string FullName, string Email)> GetFullNameById(int? employeeId)
        {
            if (!employeeId.HasValue) return ("", "");

            var empData = await _dbContext.Employees
                .Where(e => e.IdEmployee == employeeId.Value)
                .Select(e => new
                {
                    FirstName = e.FirstName,
                    MiddleName = e.MiddleName,
                    LastName = e.LastName,
                    Email = e.EmailID
                })
                .FirstOrDefaultAsync();

            if (empData == null) return ("", "");

            var fullName = $"{empData.FirstName ?? ""} {empData.MiddleName ?? ""} {empData.LastName ?? ""}".Trim();

            return (fullName, empData.Email);
        }



        private async Task<Notification> CreateNotificationforEnmployeeSalaryConfig(string approverName, string employeename, string creatorName,string  createdDateTime, NotificationConfig notificationConfig,int loggedInEmployeeId,int empId,int entityTablePrimaryKeyID,  string templateType, string? rejectRemarks)
        {

            string appNotificationText = templateType switch
            {
                "SUBMITTED" => $"Salary Configuration {""} created by {creatorName} requires your action.",
                "APPROVED" => $"Salary Configuration {employeename} APPROVED.",
                "REJECTED" => $"Salary Configuration {""} REJECTED.",
                _ => "You have a new notification."
            };


            string emailContent = notificationConfig.EmailContent;

            if (templateType == "SUBMITTED")
            {
                emailContent = emailContent
                 .Replace("#APPROVERNAME#", approverName)
                 .Replace("#SALARYEMPLOYEENAME#", employeename)
                 .Replace("#CREATORNAME#", creatorName)
                 .Replace("#CREATEDDATETIME#", createdDateTime);
            }
            else
            if (templateType == "APPROVED")
            {
                emailContent = emailContent
                    .Replace("#CREATORNAME#", creatorName)
                    .Replace("#SALARYEMPLOYEENAME#", employeename)
                    .Replace("#APPROVERNAME#", approverName)
                    .Replace("#APPROVEDDATETIME#", createdDateTime);
            }
            else if (templateType == "REJECTED")
            {
                emailContent = emailContent
                    .Replace("#SALARYCONFIGURATIONNAME#", "");
                    
            }



            string notificationLink = templateType == "SUBMITTED"
      ? "config-approvals"
      : "employee-salary-config";

            return new Notification
            {
                IdNotificationConfig = notificationConfig?.IdNotificationConfig,
                NotificationType = notificationConfig?.NotificationType,
                SentByIdEmployee = loggedInEmployeeId,
                ReceivedByIdEmployee = empId,
                EmailSubject = notificationConfig?.EmailSubject,
                EmailContent = emailContent, // now replaced
                EmailSentStatus = "SENT",
                AppNotificationText = appNotificationText, // now generated
                NotificationLink = notificationLink,
                IsReadAppNotification = false,
                LogoText=notificationConfig.LogoText,
                Status = "SENT",
                RelatedRecordID = entityTablePrimaryKeyID,
                RelatedRecordType = "EMPSALCONFIG",
                CreatedAt = DateTime.Now
            };

        }




        private async Task<Notification> CreateNotificationforOvertimeConfig(string approverName, string employeename, string creatorName, string createdDateTime, NotificationConfig notificationConfig, int loggedInEmployeeId, int empId, int entityTablePrimaryKeyID, string templateType, string? rejectRemarks)
        {

            string appNotificationText = templateType switch
            {
                "SUBMITTED" => $"An overtime transaction submitted by {creatorName} requires your action.",
                "INTERIM APPROVED" => $"Your Overtime Transaction has been APPROVED by {employeename}. Please wait for HR Approval..",
                "APPROVED" => $"Your Overtime Transaction has been APPROVED by your Manager and HR Manager",
                "REJECTED" => $"Your Overtime Transaction has been REJECTED by {employeename}.",
                _ => "You have a new notification."
            };


            string emailContent = notificationConfig.EmailContent;

            if (templateType == "SUBMITTED")
            {
                emailContent = emailContent
                 .Replace("#APPROVERNAME#", approverName)                 
                 .Replace("#CREATORNAME#", creatorName)
                 .Replace("#CREATEDDATETIME#", createdDateTime);
            }
            else
            if (templateType == "INTERIM APPROVED")
            {
                emailContent = emailContent
                    .Replace("#CREATORNAME#", creatorName)                    
                    .Replace("#APPROVERNAME#", approverName)
                    .Replace("#APPROVEDEDDATETIME#", createdDateTime);
            }
            else
            if (templateType == "APPROVED")
            { 
                emailContent = emailContent
                    .Replace("#CREATORNAME#", creatorName)                  
                    .Replace("#APPROVEDEDDATETIME#", createdDateTime);
            }
            else if (templateType == "REJECTED")
            {
                emailContent = emailContent
                    .Replace("#RECEIVEDEMPLOYEENAME#", creatorName)
                    .Replace("#REJECTEDEMPLOYEENAME#", approverName)
                    .Replace("#REJECTEDEDDATETIME#", createdDateTime)
                    .Replace("#REJECTIONREASON#", rejectRemarks);

            }



            string notificationLink = templateType == "SUBMITTED"
      ? "config-approvals"
      : "overtime-transactions";

            return new Notification
            {
                IdNotificationConfig = notificationConfig?.IdNotificationConfig,
                NotificationType = notificationConfig?.NotificationType,
                SentByIdEmployee = loggedInEmployeeId,
                ReceivedByIdEmployee = empId,
                EmailSubject = notificationConfig?.EmailSubject,
                EmailContent = emailContent, // now replaced
                EmailSentStatus = "SENT",
                LogoText=notificationConfig.LogoText,
                AppNotificationText = appNotificationText, // now generated
                NotificationLink = notificationLink,
                IsReadAppNotification = false,
                Status = "SENT",
                RelatedRecordID = entityTablePrimaryKeyID,
                RelatedRecordType = "OVERTIME",
                CreatedAt = DateTime.Now
            };

        }




        private async Task<Notification> CreateNotificationforSalaryTemplate(int fromId, int toId, NotificationConfig config, string approverName, string creatorName, int relatedId,string createdDateTime, string salaryTemplateName,string templateType,string? rejectRemarks)
        {
          
            string appNotificationText = templateType switch
            {
                "SUBMITTED" => $"Salary Template {salaryTemplateName} created by {creatorName} requires your action.",
                "APPROVED" => $"Salary Template {salaryTemplateName} APPROVED.",
                "REJECTED" => $"Salary Template {salaryTemplateName} REJECTED.",
                _ => "You have a new notification."
            };


            string emailContent = config.EmailContent;
           
            if (templateType == "SUBMITTED")
            {
                emailContent = emailContent
                 .Replace("#APPROVERNAME#", approverName)
                 .Replace("#SALARYTEMPLATENAME#", salaryTemplateName)
                 .Replace("#CREATORNAME#", creatorName)
                 .Replace("#CREATEDDATETIME#", createdDateTime);
            }
            else
            if (templateType == "APPROVED")
            {
                emailContent = emailContent
                    .Replace("#CREATORNAME#", creatorName)
                    .Replace("#SALARYTEMPLATENAME#", salaryTemplateName)
                    .Replace("#APPROVERNAME#", approverName)
                    .Replace("#APPROVEDDATETIME#", createdDateTime);
            }
            else  if (templateType == "REJECTED")
            {
                emailContent = emailContent
                    .Replace("#CREATORNAME#", creatorName)
                    .Replace("#SALARYTEMPLATENAME#", salaryTemplateName)
                    .Replace("#APPROVERNAME#", approverName)
                    .Replace("#REJECTIONREASON#", rejectRemarks ?? "No reason provided.");
            }



            string notificationLink = templateType == "SUBMITTED"
      ? "config-approvals"
      : "salary-templates";

            return new Notification
            {
                IdNotificationConfig = config?.IdNotificationConfig,
                NotificationType = config?.NotificationType,
                SentByIdEmployee = fromId,
                ReceivedByIdEmployee = toId,
                EmailSubject = config?.EmailSubject,
                EmailContent = emailContent, // now replaced
                LogoText=config.LogoText,
                EmailSentStatus = "SENT",
                AppNotificationText = appNotificationText, // now generated
                NotificationLink = notificationLink,
                IsReadAppNotification = false,
                Status = "SENT",
                RelatedRecordID = relatedId,
                RelatedRecordType = "SALTEM",
                CreatedAt = DateTime.Now
            };

        }



        /// <summary>
        /// Fetches target employees based on the approval authority type.
        /// </summary>
        private async Task<List<int>> GetTargetEmployees(WorkFlowConfigDetails workflowConfigDetails)
        {
            if (workflowConfigDetails.ApprovalAuthorityType == "ROLE")
            {
                var designationId = workflowConfigDetails.ApprovalAuthorityID;
                return await _dbContext.Employees
                    .Where(e => e.IdDesignation == designationId)
                    .Select(e => e.IdEmployee)
                    .ToListAsync();
            }
            else if (workflowConfigDetails.ApprovalAuthorityType == "REPOFFICER" && workflowConfigDetails.ApprovalAuthorityID.HasValue)
            {
                return new List<int> { workflowConfigDetails.ApprovalAuthorityID.Value }; 
            }

            return new List<int>();
        }

        public async Task<IEnumerable<ConfigApprovalsDto>> GetConfigApprovalsList(DateTime fromDate,string? actionStatus = null, string? entityCode = null, string? targetIdEmployee = null)
        {
            var query = new StringBuilder(@"
           SELECT 
    asb.IdApprovalWorkFlow,
	wc.EntityName,
    asb.EntityTablePrimaryKeyID, 
    asb.EntityCode, 
    asb.CycleIndex, 
	CONCAT(FirstName, ' ', MiddleName, ' ', LastName) AS CreatedBy,
    asb.SentDate, 
	asb.TargetIdEmployee,
    asb.ActionStatus,
    asb.RejectionRemarks,
    CASE 
        WHEN asb.EntityCode = 'SALTEM' 
            THEN (SELECT SalaryTemplateName FROM SalaryTemplates WHERE IdSalaryTemplate = asb.EntityTablePrimaryKeyID)
        WHEN asb.EntityCode = 'EMPSALCONFIG' 
			THEN(select  CONCAT(e.FirstName, ' ', e.MiddleName, ' ', e.LastName)  from EmployeeSalaryConfig ec inner join Employees e on ec.IdEmployee=e.IdEmployee where ec.IdEmployeeSalaryConfig = asb.EntityTablePrimaryKeyID)

        WHEN asb.EntityCode = 'OVERTIME' 
           THEN(select  CONCAT(e.FirstName, ' ', e.MiddleName, ' ', e.LastName)  from OvertimeTransactions ot inner join Employees e on ot.IdEmployee=e.IdEmployee where ot.IdOvertimeTransaction = asb.EntityTablePrimaryKeyID)
        ELSE NULL
    END AS Details

FROM ApprovalWorkFlowAllocations asb
left join WorkFlowConfig wc on asb.IdWorkFlowConfig = wc.IdWorkFlowConfig
left join Employees e on e.IdEmployee = asb.SourceIdEmployee
            INNER JOIN (
                SELECT 
                    EntityTablePrimaryKeyID, 
                    EntityCode, 
                    MAX(CycleIndex) AS MaxCycleIndex
                FROM ApprovalWorkFlowAllocations
                GROUP BY EntityTablePrimaryKeyID, EntityCode
            ) maxCycles 
            ON asb.EntityTablePrimaryKeyID = maxCycles.EntityTablePrimaryKeyID 
               AND asb.EntityCode = maxCycles.EntityCode 
               AND asb.CycleIndex = maxCycles.MaxCycleIndex
            WHERE 1=1 "); // Placeholder to append dynamic conditions

            var parameters = new DynamicParameters();

            // Fetch entity codes from appsettings.json
            var allowedEntityCodes = _configuration.GetSection("ConfigApproval:EntityCodes").Get<List<string>>();

            if (allowedEntityCodes?.Any() == true)
            {
                query.Append(" AND asb.EntityCode IN @EntityCodes ");
                parameters.Add("EntityCodes", allowedEntityCodes);
            }

            
            if (!string.IsNullOrEmpty(actionStatus))
            {
                if (actionStatus.Equals("SUBMITTED", StringComparison.OrdinalIgnoreCase))
                {
                    // Include SUBMITTED and Interim Approved statuses
                    query.Append(@"
            AND (
                asb.ActionStatus = 'SUBMITTED'
                OR asb.ActionStatus = 'INTERIM APPROVED'
            )");
                }
                else
                {
                    query.Append(" AND asb.ActionStatus = @ActionStatus ");
                    parameters.Add("ActionStatus", actionStatus);
                }
            }


            if (!string.IsNullOrEmpty(entityCode))
            {
                query.Append(" AND asb.EntityCode = @EntityCode ");
                parameters.Add("EntityCode", entityCode);
            }

            if (!string.IsNullOrEmpty(targetIdEmployee))
            {
                query.Append(@" AND (
                asb.TargetIdEmployee = @TargetIdEmployee
                OR asb.TargetIdEmployee LIKE @TargetIdEmployeePrefix
                OR asb.TargetIdEmployee LIKE @TargetIdEmployeeSuffix
                OR asb.TargetIdEmployee LIKE @TargetIdEmployeeMiddle
            )");
                parameters.Add("TargetIdEmployee", targetIdEmployee);
                parameters.Add("TargetIdEmployeePrefix", $"{targetIdEmployee},%");
                parameters.Add("TargetIdEmployeeSuffix", $"%,{targetIdEmployee}");
                parameters.Add("TargetIdEmployeeMiddle", $"%,{targetIdEmployee},%");
            }
            query.Append(" AND asb.SentDate >= @FromDate ");
            parameters.Add("FromDate", fromDate);

            query.Append(" ORDER BY wc.EntityName ASC;");

            try
            {
                using (var connection = _dbContext.Database.GetDbConnection())
                {
                    if (connection.State == ConnectionState.Closed)
                        await connection.OpenAsync();

                    return await connection.QueryAsync<ConfigApprovalsDto>(query.ToString(),parameters);
                }
            }
            catch (Exception ex)
            {
                //_logger.LogError(ex, "Error fetching approval workflows.");
                throw new Exception("An error occurred while fetching approval workflows. Please try again later.");
            }
        }
    }
}
