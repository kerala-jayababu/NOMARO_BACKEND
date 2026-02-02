using Dapper;
using Georgetown_International_Academy.API.Database;
using Georgetown_Internationsl_Academy.API.DTO;
using Georgetown_Internationsl_Academy.API.Helpers;
using Georgetown_Internationsl_Academy.API.Models;
using Georgetown_Internationsl_Academy.API.Services.Interface;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json.Linq;
using Org.BouncyCastle.Ocsp;
using Org.BouncyCastle.Tls;
using System.Collections.Generic;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Text.Json;
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

        public async Task<string> InitiateApprovalWorkflow(int entityTablePrimaryKeyID, string entityCode, int loggedInEmployeeId, string? status, decimal? LeavePassageAmount, string? rejectReason, int count=1)
        {

            try
            {
                var currentRecord = await _dbContext.ApprovalWorkFlowAllocations
                             .Where(a => a.EntityTablePrimaryKeyID == entityTablePrimaryKeyID && a.EntityCode == entityCode)
                             .OrderByDescending(a => a.CycleIndex)
                             .ThenByDescending(a => a.LevelNumber)
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
                    var targetEmployees = await GetTargetEmployees(firstLevel, loggedInEmployeeId);

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
                        //ActionStatus = actionStatussubmitted,
                        SentDate = DateTime.Now
                    };

                    await _dbContext.ApprovalWorkFlowAllocations.AddAsync(newRecord);
                    await _dbContext.SaveChangesAsync();
                    if (actionStatussubmitted == "FINAL APPROVED")
                    {
                        await UpdateEntityStatus(entityTablePrimaryKeyID, entityCode, "FINAL APPROVED", 1,1, null, null, null,count, rejectReason,null);
                        return "Approval workflow initiated.";
                    }
                    else
                    {
                        await UpdateEntityStatus(entityTablePrimaryKeyID, entityCode, "SUBMITTED", 1,1, loggedInEmployeeId, targetEmployeeIds, null, count, rejectReason,null);
                    }

                    return "Approval workflow initiated.";
                }

                // Step 3: Validate logged-in employee
                if (currentRecord.TargetIdEmployee == null ||
                    !currentRecord.TargetIdEmployee.Split(',').Contains(loggedInEmployeeId.ToString()))
                {
                    return "Error:You are not authorized to approve/reject this record.";
                }
                var workflowConfigDetailsFinal = await _dbContext.WorkFlowConfigDetails
                      .FirstOrDefaultAsync(w => w.IdWorkFlowConfig == currentRecord.IdWorkFlowConfig && w.LevelNumber == currentRecord.LevelNumber);
                // Step 4: Update current level's status
                currentRecord.ActionedBy = loggedInEmployeeId;
                currentRecord.ActionStatus = status == "REJECTED" ? "REJECTED" : workflowConfigDetailsFinal.ApprovalStatusName;
                currentRecord.ActionDate = DateTime.Now;
                currentRecord.RejectionRemarks = status == "REJECTED" ? rejectReason : null;
                await _dbContext.SaveChangesAsync();

                // Step 5: Handle rejection
                if (status == "REJECTED")
                {

                    await UpdateEntityStatus(entityTablePrimaryKeyID, entityCode, "REJECTED", currentRecord.CycleIndex, workflowConfigDetailsFinal.LevelNumber, loggedInEmployeeId, null, LeavePassageAmount,count, rejectReason,currentRecord.SourceIdEmployee);
                    //await transaction.CommitAsync();
                    return "Record rejected successfully. Workflow terminated.";
                }

                // Step 6: Increment level and check for the next level
                var nextLevelNumber = currentRecord.LevelNumber + 1;
                var workflowConfigDetails = await _dbContext.WorkFlowConfigDetails
                    .FirstOrDefaultAsync(w => w.IdWorkFlowConfig == currentRecord.IdWorkFlowConfig && w.LevelNumber == nextLevelNumber);

                if (workflowConfigDetails != null)
                {
                    var targetEmployeesForNextLevel = await GetTargetEmployees(workflowConfigDetails, loggedInEmployeeId);

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
                        //ActionStatus = actionStatus,
                        SourceIdEmployee = loggedInEmployeeId,
                        TargetIdEmployee = targetEmployeeIdsForNextLevel,
                        SentDate = DateTime.Now
                    };

                    await _dbContext.ApprovalWorkFlowAllocations.AddAsync(newNextLevelRecord);
                    await UpdateEntityStatus(entityTablePrimaryKeyID, entityCode, workflowConfigDetailsFinal.ApprovalStatusName, newNextLevelRecord.CycleIndex, nextLevelNumber,loggedInEmployeeId, targetEmployeeIdsForNextLevel,  LeavePassageAmount,count, rejectReason,null);
                    await _dbContext.SaveChangesAsync();
                }
                else
                {
                    await UpdateEntityStatus(entityTablePrimaryKeyID, entityCode, workflowConfigDetailsFinal.ApprovalStatusName, currentRecord.CycleIndex, status == "REJECTED" ? 0 :  99 , loggedInEmployeeId, null, LeavePassageAmount,count, rejectReason,currentRecord.SourceIdEmployee);
                    await _dbContext.SaveChangesAsync();
                }

                // Step 7: Insert the next level record


                //await transaction.CommitAsync();

                return "Record approved and moved to the next level.";
            }
            catch (Exception ex)
            {
                //await transaction.RollbackAsync();
                return $"Error processing approval workflow: {ex.Message}";
            }
        }

        public async Task AddUpdateWorkFlowApprovalForLeave(int idLeaveApplication,int idEmployee, EmployeeLeaveSetupDetailDto empLvConfigDetails)
        {
            try
            {
                string entityCode = "LEAVE" + "_" + empLvConfigDetails.IdLeaveTemplateDetail;

                var wfConfig = await _dbContext.WorkFlowConfig
                    .FirstOrDefaultAsync(et => et.EntityCode == entityCode);

                if (wfConfig == null)
                    return;

                var wfConfigDetails = await _dbContext.WorkFlowConfigDetails
                    .Where(wf => wf.IdWorkFlowConfig == wfConfig.IdWorkFlowConfig)
                    .OrderBy(wf => wf.LevelNumber)
                    .ToListAsync();

                if (!wfConfigDetails.Any())
                    return;

                var employee = await _dbContext.Employees
                    .FirstOrDefaultAsync(em => em.IdEmployee == idEmployee);

                int? reportingOfficerId = employee?.ReportingTo;

                int cycleIndex = 1;

                foreach (var wfd in wfConfigDetails)
                {
                    string targetEmployeeIds = string.Empty;

                    if (wfd.ApprovalAuthorityType == "REPOFFICER")
                    {
                        if (!reportingOfficerId.HasValue)
                            continue;

                        targetEmployeeIds = reportingOfficerId.Value.ToString();
                    }
                    else
                    {
                        var empIds = await _dbContext.Employees
                            .Where(em => em.IdDesignation == wfd.ApprovalAuthorityID)
                            .Select(em => em.IdEmployee)
                            .ToListAsync();

                        if (!empIds.Any())
                            continue;

                        targetEmployeeIds = string.Join(",", empIds);
                    }

                    var approvalFlow = new ApprovalWorkFlowAllocation
                    {
                        IdWorkFlowConfig = wfConfig.IdWorkFlowConfig,
                        EntityCode = wfConfig.EntityCode,
                        EntityTablePrimaryKeyID = idLeaveApplication,
                        CycleIndex = cycleIndex,
                        LevelNumber = wfd.LevelNumber,
                        SourceIdEmployee = idEmployee,
                        TargetIdEmployee = targetEmployeeIds,
                        SentDate = DateTime.Now
                    };

                    await _dbContext.ApprovalWorkFlowAllocations.AddAsync(approvalFlow);
                }

                await _dbContext.SaveChangesAsync();
            }
            catch(Exception ee)
            {
                int p = 100;
            }
        }


        private async Task UpdateEntityStatus(int entityTablePrimaryKeyID, string entityCode, string finalStatus, int cycleIndex,int? nextLevelNumber, int? loggedInEmployeeId, string targetEmployeeIdsForNextLevel, decimal? LeavePassageAmount,int count,string? rejectReason,int? sourceIdEmployee)
        {
            if (entityCode == _configuration["WorkflowEntityCodes:SalaryTemplate"])
            {
                var entity = await _dbContext.SalaryTemplates.FindAsync(entityTablePrimaryKeyID);
                var workflowConfig = await _dbContext.WorkFlowConfig.Where(x => x.EntityCode == entityCode).FirstOrDefaultAsync();
                if ( finalStatus == "REJECTED"|| nextLevelNumber ==99)
                {
                    targetEmployeeIdsForNextLevel = sourceIdEmployee.ToString();
                }
                if (entity != null)
                {
                    entity.ApprovalStatus = finalStatus;
                    await _dbContext.SaveChangesAsync();
                }
                var (senderName, senderEmail) = await GetFullNameById(loggedInEmployeeId);
                var employeeIdList = ParseEmployeeIds(targetEmployeeIdsForNextLevel);
                var employees = await GetEmployeesByIds(employeeIdList);
                var receiverNames = string.Join(", ", employees.Select(e => e["Name"]));
                var receiverEmails = employees.Select(e => e["Email"]).Where(x => !string.IsNullOrWhiteSpace(x)).ToList();
                var notificationConfig = await GetNotificationConfigForEntity(entityCode, nextLevelNumber ?? 0, senderName, receiverNames, rejectReason);

                foreach (var emp in employees)
                {
                    var toEmail = emp["Email"];
                    if (!string.IsNullOrWhiteSpace(toEmail))
                    {
                        EmailService.SendMail(toEmail, notificationConfig.EmailSubject, notificationConfig.EmailContent);
                    }
                    var employeeDetails = await _dbContext.Employees.Where(x => x.EmailID == toEmail).FirstOrDefaultAsync();

                    if (employeeDetails != null)
                    {
                        Notification obj = new Notification
                        {
                            IdNotificationConfig = notificationConfig.IdNotificationConfig,
                            NotificationType = notificationConfig.NotificationType,
                            SentByIdEmployee = loggedInEmployeeId,
                            ReceivedByIdEmployee = employeeDetails.IdEmployee,
                            AppNotificationText = notificationConfig.AppNotificationText,
                            EmailSubject = notificationConfig.EmailSubject,
                            EmailContent = notificationConfig.EmailContent,
                            EmailSentStatus = "SENT",
                            IsReadAppNotification = false,
                            CreatedAt = DateTime.Now,
                            Status = "SENT",
                            RelatedRecordID = entityTablePrimaryKeyID,
                            RelatedRecordType = entityCode,
                            LogoText = notificationConfig.LogoText,
                            NotificationLink = notificationConfig.NotificationLink
                        };

                        await _dbContext.Notifications.AddAsync(obj);
                        await _dbContext.SaveChangesAsync();
                    }
                }

            }
            else if (entityCode == _configuration["WorkflowEntityCodes:EmployeeSalaryConfig"])
            {
                var entity = await _dbContext.EmployeeSalaryConfig.FindAsync(entityTablePrimaryKeyID);
                var workflowConfig = await _dbContext.WorkFlowConfig.Where(x => x.EntityCode == entityCode).FirstOrDefaultAsync();
                if ( finalStatus == "REJECTED" || nextLevelNumber == 99)
                {
                    targetEmployeeIdsForNextLevel = entity.CreatedBy.ToString();
                }
                //var employeedetails = await _dbContext.Employees.Where(x => x.IdEmployee == entity.IdEmployee).FirstOrDefaultAsync();
                //var employeename = string.Concat(employeedetails.FirstName, employeedetails.MiddleName, employeedetails.LastName);
                if (entity != null)
                {
                    entity.ApprovalStatus = finalStatus;
                    await _dbContext.SaveChangesAsync();
                }

                var (senderName, senderEmail) = await GetFullNameById(loggedInEmployeeId);
                var employeeIdList = ParseEmployeeIds(targetEmployeeIdsForNextLevel);
                var employees = await GetEmployeesByIds(employeeIdList);
                var receiverNames = string.Join(", ", employees.Select(e => e["Name"]));
                var receiverEmails = employees.Select(e => e["Email"]).Where(x => !string.IsNullOrWhiteSpace(x)).ToList();
                var notificationConfig = await GetNotificationConfigForEntity(entityCode, nextLevelNumber ?? 0, senderName, receiverNames, rejectReason);
                foreach (var emp in employees)
                {
                    var toEmail = emp["Email"];
                    if (!string.IsNullOrWhiteSpace(toEmail))
                    {
                        EmailService.SendMail(toEmail, notificationConfig.EmailSubject, notificationConfig.EmailContent);
                    }
                    var employeeDetails = await _dbContext.Employees.Where(x => x.EmailID == toEmail).FirstOrDefaultAsync();

                    if (employeeDetails != null)
                    {
                        Notification obj = new Notification
                        {
                            IdNotificationConfig = notificationConfig.IdNotificationConfig,
                            NotificationType = notificationConfig.NotificationType,
                            SentByIdEmployee = loggedInEmployeeId,
                            ReceivedByIdEmployee = employeeDetails.IdEmployee,
                            AppNotificationText = notificationConfig.AppNotificationText,
                            EmailSubject = notificationConfig.EmailSubject,
                            EmailContent = notificationConfig.EmailContent,
                            EmailSentStatus = "SENT",
                            IsReadAppNotification = false,
                            CreatedAt = DateTime.Now,
                            Status = "SENT",
                            RelatedRecordID = entityTablePrimaryKeyID,
                            RelatedRecordType = entityCode,
                            LogoText = notificationConfig.LogoText,
                            NotificationLink = notificationConfig.NotificationLink
                        };

                        await _dbContext.Notifications.AddAsync(obj);
                        await _dbContext.SaveChangesAsync();
                    }
                }

            }
            else if (entityCode == _configuration["WorkflowEntityCodes:OVERTIME"])
            {
                var entity = await _dbContext.OvertimeTransactions.FindAsync(entityTablePrimaryKeyID);
                var workflowConfig = await _dbContext.WorkFlowConfig.Where(x => x.EntityCode == entityCode).FirstOrDefaultAsync();
                if( finalStatus == "REJECTED" || nextLevelNumber == 99)
                {
                    targetEmployeeIdsForNextLevel = entity.CreatedBy.ToString();
                }
                
                if (entity != null)
                {
                    entity.ApprovalStatus = finalStatus;
                    await _dbContext.SaveChangesAsync();
                }
                
                var (senderName, senderEmail) = await GetFullNameById(loggedInEmployeeId);
                var employeeIdList = ParseEmployeeIds(targetEmployeeIdsForNextLevel);
                var employees = await GetEmployeesByIds(employeeIdList);
                var receiverNames = string.Join(", ", employees.Select(e => e["Name"]));
                var receiverEmails = employees.Select(e => e["Email"]).Where(x => !string.IsNullOrWhiteSpace(x)).ToList();
                var notificationConfig = await GetNotificationConfigForEntity(entityCode,nextLevelNumber ?? 0,senderName,receiverNames, rejectReason);
                foreach (var emp in employees)
                {
                    var toEmail = emp["Email"];
                    if (!string.IsNullOrWhiteSpace(toEmail))
                    {
                         EmailService.SendMail(toEmail,notificationConfig.EmailSubject,notificationConfig.EmailContent);
                    }
                    var employeeDetails = await _dbContext.Employees.Where(x => x.EmailID == toEmail).FirstOrDefaultAsync();

                    if (employeeDetails != null)
                    {
                        Notification obj = new Notification
                        {
                            IdNotificationConfig = notificationConfig.IdNotificationConfig,
                            NotificationType = notificationConfig.NotificationType,
                            SentByIdEmployee = loggedInEmployeeId,
                            ReceivedByIdEmployee = employeeDetails.IdEmployee,
                            AppNotificationText = notificationConfig.AppNotificationText,
                            EmailSubject = notificationConfig.EmailSubject,
                            EmailContent = notificationConfig.EmailContent,
                            EmailSentStatus = "SENT",
                            IsReadAppNotification = false,
                            CreatedAt = DateTime.Now,
                            Status = "SENT",
                            RelatedRecordID = entityTablePrimaryKeyID,
                            RelatedRecordType = entityCode,
                            LogoText = notificationConfig.LogoText,
                            NotificationLink = notificationConfig.NotificationLink
                        };

                        await _dbContext.Notifications.AddAsync(obj);
                        await _dbContext.SaveChangesAsync();
                    }
                }

            }
            else if (entityCode == _configuration["WorkflowEntityCodes:EmployeeServiceChange"])
            {
                var entity = await _dbContext.EmployeeServiceChanges.FindAsync(entityTablePrimaryKeyID);

                if (entity == null)
                    return;

                // If rejected or completed, route back to requester (optional)
                if (finalStatus == "REJECTED" || nextLevelNumber == 99)
                {
                    // if you want, you can ensure the record is considered back to creator/requester
                    // targetEmployeeIdsForNextLevel = entity.CreatedBy.ToString();
                }

                entity.ApprovalStatus = finalStatus;

                // Set "approved" info only when final approval is reached.
                // Adjust conditions if your system uses "FINAL APPROVED" / "APPROVED" differently.
                bool isFinalApproved =
                    finalStatus == "FINAL APPROVED" ||
                    finalStatus == "APPROVED" ||
                    nextLevelNumber == 99;

                if (isFinalApproved)
                {
                    entity.ApprovedBy = loggedInEmployeeId;
                    entity.ApprovedDate = DateTime.Now;
                }
                else if (finalStatus == "REJECTED")
                {
                    // optional: clear approval info on rejection
                    entity.ApprovedBy = null;
                    entity.ApprovedDate = null;
                }

                // optional: always track update
                entity.UpdatedBy = loggedInEmployeeId;
                entity.UpdatedAt = DateTime.Now;

                await _dbContext.SaveChangesAsync();
            }
            else if (entityCode == _configuration["WorkflowEntityCodes:LEAVEPASS"])
            {
                var entity = await _dbContext.LeavePassages.FindAsync(entityTablePrimaryKeyID);
                var leavepassageamount = await _dbContext.LeavePassageAmounts.Where(x => x.IdEmployee == entity.IdEmployee && x.IdFinancialYear == entity.IdFinancialYear).FirstOrDefaultAsync();
                var employeedetails = await _dbContext.Employees.Where(x => x.IdEmployee == entity.IdEmployee).FirstOrDefaultAsync();
                var employeename = string.Concat(employeedetails.FirstName, employeedetails.MiddleName, employeedetails.LastName);
                if (entity != null)
                {

                    if (nextLevelNumber ==99)
                    {
                        entity.LeavePassageAmount = leavepassageamount?.LeavePassageAmount ?? 0;
                    }

                    entity.ApprovalStatus = finalStatus;
                    await _dbContext.SaveChangesAsync();
                }
                var workflowConfig = await _dbContext.WorkFlowConfig.Where(x => x.EntityCode == entityCode).FirstOrDefaultAsync();
                if ( finalStatus == "REJECTED" || nextLevelNumber == 99)
                {
                    targetEmployeeIdsForNextLevel = entity.IdEmployee.ToString();
                }
                var (senderName, senderEmail) = await GetFullNameById(loggedInEmployeeId);
                var employeeIdList = ParseEmployeeIds(targetEmployeeIdsForNextLevel);
                var employees = await GetEmployeesByIds(employeeIdList);
                var receiverNames = string.Join(", ", employees.Select(e => e["Name"]));
                var receiverEmails = employees.Select(e => e["Email"]).Where(x => !string.IsNullOrWhiteSpace(x)).ToList();
                var notificationConfig = await GetNotificationConfigForEntity(entityCode, nextLevelNumber ?? 0, senderName, receiverNames, rejectReason);
                foreach (var emp in employees)
                {
                    if (notificationConfig != null)
                    {

                    
                    var toEmail = emp["Email"];
                    if (!string.IsNullOrWhiteSpace(toEmail))
                    {
                        EmailService.SendMail(toEmail, notificationConfig.EmailSubject, notificationConfig.EmailContent);
                    }
                    var employeeDetails = await _dbContext.Employees.Where(x => x.EmailID == toEmail).FirstOrDefaultAsync();

                    if (employeeDetails != null)
                    {
                        Notification obj = new Notification
                        {
                            IdNotificationConfig = notificationConfig.IdNotificationConfig,
                            NotificationType = notificationConfig.NotificationType,
                            SentByIdEmployee = loggedInEmployeeId,
                            ReceivedByIdEmployee = employeeDetails.IdEmployee,
                            AppNotificationText = notificationConfig.AppNotificationText,
                            EmailSubject = notificationConfig.EmailSubject,
                            EmailContent = notificationConfig.EmailContent,
                            EmailSentStatus = "SENT",
                            IsReadAppNotification = false,
                            CreatedAt = DateTime.Now,
                            Status = "SENT",
                            RelatedRecordID = entityTablePrimaryKeyID,
                            RelatedRecordType = entityCode,
                            LogoText = notificationConfig.LogoText,
                            NotificationLink = notificationConfig.NotificationLink
                        };

                        await _dbContext.Notifications.AddAsync(obj);
                        await _dbContext.SaveChangesAsync();
                    }
                      }
                }
            }
            else if (entityCode == _configuration["WorkflowEntityCodes:EMPSALGEN"])
            {
                entityCode = "SALARYGEN";
                var entity = await _dbContext.EmployeeSalaries.FindAsync(entityTablePrimaryKeyID);
                if (entity != null)
                {
                    entity.ApprovalStatus = finalStatus;
                    entity.ApprovedDate = DateTime.Now;
                    entity.IdApprovedBy = loggedInEmployeeId;
                    await _dbContext.SaveChangesAsync();
                }
                if (finalStatus == "REJECTED" || nextLevelNumber == 99)
                {
                   
                    targetEmployeeIdsForNextLevel = entity.CreatedBy.ToString();
                }

                if (count == 1)
                {
                    var employeeIdList = ParseEmployeeIds(targetEmployeeIdsForNextLevel);
                    var (senderName, senderEmail) = await GetFullNameById(loggedInEmployeeId);
                    var employees = await GetEmployeesByIds(employeeIdList);
                    var receiverNames = string.Join(", ", employees.Select(e => e["Name"]));
                    var notificationConfig = await GetNotificationConfigForEntity(entityCode, nextLevelNumber ?? 0, senderName, receiverNames, rejectReason);
                    foreach (var empId in employees)
                    {

                        var toEmail = empId["Email"];
                        if (!string.IsNullOrWhiteSpace(toEmail))
                        {
                            EmailService.SendMail(toEmail, notificationConfig.EmailSubject, notificationConfig.EmailContent);
                        }
                        var employeeDetails = await _dbContext.Employees.Where(x => x.EmailID == toEmail).FirstOrDefaultAsync();

                        if (employeeDetails != null)
                        {
                            Notification obj = new Notification
                            {
                                IdNotificationConfig = notificationConfig.IdNotificationConfig,
                                NotificationType = notificationConfig.NotificationType,
                                SentByIdEmployee = loggedInEmployeeId,
                                ReceivedByIdEmployee = employeeDetails.IdEmployee,
                                AppNotificationText = notificationConfig.AppNotificationText,
                                EmailSubject = notificationConfig.EmailSubject,
                                EmailContent = notificationConfig.EmailContent,
                                EmailSentStatus = "SENT",
                                IsReadAppNotification = true,
                                CreatedAt = DateTime.Now,
                                Status = "SENT",
                                ReadAt = DateTime.Now,                                
                                RelatedRecordID = entityTablePrimaryKeyID,
                                RelatedRecordType = entityCode,
                                LogoText = notificationConfig.LogoText,
                                NotificationLink = notificationConfig.NotificationLink
                            };

                            await _dbContext.Notifications.AddAsync(obj);
                            await _dbContext.SaveChangesAsync();

                        }
                        await _dbContext.SaveChangesAsync();
                    }
                }

              

            }
            else if (entityCode == _configuration["WorkflowEntityCodes:EmployeeAction"])
            {
                var entity = await _dbContext.EmployeeActions.FindAsync(entityTablePrimaryKeyID);

                if (entity != null)
                {
                    entity.ApprovalStatus = finalStatus;
                    await _dbContext.SaveChangesAsync();
                }

                if (finalStatus == "REJECTED" || nextLevelNumber == 99)
                    targetEmployeeIdsForNextLevel = entity.CreatedBy.ToString();

                var (senderName, senderEmail) = await GetFullNameById(loggedInEmployeeId);
                var employeeIdList = ParseEmployeeIds(targetEmployeeIdsForNextLevel);
                var employees = await GetEmployeesByIds(employeeIdList);
                var receiverNames = string.Join(", ", employees.Select(e => e["Name"]));
                var notificationConfig = await GetNotificationConfigForEntity(entityCode, nextLevelNumber ?? 0, senderName, receiverNames, rejectReason);

                foreach (var emp in employees)
                {
                    var toEmail = emp["Email"];
                    if (!string.IsNullOrWhiteSpace(toEmail))
                        EmailService.SendMail(toEmail, notificationConfig.EmailSubject, notificationConfig.EmailContent);

                    var employeeDetails = await _dbContext.Employees.FirstOrDefaultAsync(x => x.EmailID == toEmail);
                    if (employeeDetails != null)
                    {
                        await _dbContext.Notifications.AddAsync(new Notification
                        {
                            IdNotificationConfig = notificationConfig.IdNotificationConfig,
                            NotificationType = notificationConfig.NotificationType,
                            SentByIdEmployee = loggedInEmployeeId,
                            ReceivedByIdEmployee = employeeDetails.IdEmployee,
                            AppNotificationText = notificationConfig.AppNotificationText,
                            EmailSubject = notificationConfig.EmailSubject,
                            EmailContent = notificationConfig.EmailContent,
                            EmailSentStatus = "SENT",
                            IsReadAppNotification = false,
                            CreatedAt = DateTime.Now,
                            Status = "SENT",
                            RelatedRecordID = entityTablePrimaryKeyID,
                            RelatedRecordType = entityCode
                        });

                        await _dbContext.SaveChangesAsync();
                    }
                }
            }
            else if (!string.IsNullOrWhiteSpace(entityCode) && entityCode.Contains("LEAVE_", StringComparison.OrdinalIgnoreCase))
            {
                var leaveApp = await _dbContext.LeaveApplications
                    .FirstOrDefaultAsync(x => x.IdLeaveApplication == entityTablePrimaryKeyID);

                if (leaveApp == null) return;

                // update status
                leaveApp.ApprovalStatus = finalStatus;

                // final approval check (your existing logic)
                var isFinalApproved =
                    finalStatus == "FINAL APPROVED" ||
                    finalStatus == "APPROVED" ||
                    nextLevelNumber == 99;

                if (isFinalApproved)
                {
                    leaveApp.ApprovedBy = loggedInEmployeeId;
                }
                else if (finalStatus == "REJECTED")
                {
                    leaveApp.ApprovedBy = null;
                }

                await _dbContext.SaveChangesAsync();

                // If rejected/final, notify employee
                if (finalStatus == "REJECTED" || nextLevelNumber == 99)
                    targetEmployeeIdsForNextLevel = leaveApp.IdEmployee.ToString();

                // --- notifications ---
                var (senderName, senderEmail) = await GetFullNameById(loggedInEmployeeId);                             
                List<Dictionary<string, string>> employees;             
                bool isRejected = finalStatus == "REJECTED";
                var CreatorName = await GetEmployeesByIds(new List<int> { leaveApp.IdEmployee });
                var CreatorNames = string.Join(", ", CreatorName.Select(e => e["Name"]));
                if (isFinalApproved || isRejected)
                {
                    // Send to leave applicant
                    employees = await GetEmployeesByIds(new List<int> { leaveApp.IdEmployee });
                }
                else
                {
                    // Send to next approver(s)
                    var employeeIdList = ParseEmployeeIds(targetEmployeeIdsForNextLevel);
                    if (!employeeIdList.Contains(leaveApp.IdEmployee))
                        employeeIdList.Add(leaveApp.IdEmployee);
                    employees = await GetEmployeesByIds(employeeIdList);
                }

                // Receiver names (used in template)
                var receiverNames = string.Join(", ", employees.Select(e => e["Name"]));

                int levelNumberForNotification = nextLevelNumber ?? 0;
                var creator = (await GetEmployeesByIds(new List<int> { leaveApp.IdEmployee })).FirstOrDefault();
                var creatorEmail = creator != null && creator.ContainsKey("Email") ? creator["Email"] : null;
                // ✅ Choose template entity code (NEW RULE)
                string leaveEntityCodeForNotification;
                string approvalWorkflowHtml = "";
                if (finalStatus == "REJECTED")
                {
                    leaveEntityCodeForNotification = "LEAVE"; // Rejected template
                }
                else if (nextLevelNumber == 99)
                {
                    leaveEntityCodeForNotification = "LEAVE_FINALAPPROVAL"; // Final approved template
                }
                else
                {
                    leaveEntityCodeForNotification = "LEAVE_MULTILEVEL"; // Pending/multilevel template
                    approvalWorkflowHtml = BuildApprovalWorkflowHtml(leaveApp.LeaveApprovalDetails);
                }

                var notificationConfig = await GetNotificationConfigForLevaeApplicationEntity(
                    leaveEntityCodeForNotification,
                    levelNumberForNotification,
                    senderName,
                    receiverNames,
                    rejectReason: finalStatus == "REJECTED" ? rejectReason : null,
                    employeeName: CreatorNames,              // better: pass actual employee name if you have it
                    leaveType: leaveApp.LeaveTypeName,
                    fromDate: leaveApp.FromDate,
                    toDate: leaveApp.ToDate,
                    totalDays: leaveApp.TotalLeaveDays,
                    reason: leaveApp.Reason,
                    approvalWorkflow: approvalWorkflowHtml
                );

                if (notificationConfig == null) return;

                foreach (var emp in employees)
                {
                    var toEmail = emp["Email"];
                    if (!string.IsNullOrWhiteSpace(toEmail))
                        EmailService.SendMail(toEmail, notificationConfig.EmailSubject, notificationConfig.EmailContent);

                    var employeeDetails = await _dbContext.Employees.FirstOrDefaultAsync(x => x.EmailID == toEmail);
                    if (employeeDetails != null)
                    {
                        await _dbContext.Notifications.AddAsync(new Notification
                        {
                            IdNotificationConfig = notificationConfig.IdNotificationConfig,
                            NotificationType = notificationConfig.NotificationType,
                            SentByIdEmployee = loggedInEmployeeId,
                            ReceivedByIdEmployee = employeeDetails.IdEmployee,
                            AppNotificationText = notificationConfig.AppNotificationText,
                            EmailSubject = notificationConfig.EmailSubject,
                            EmailContent = notificationConfig.EmailContent,
                            EmailSentStatus = "SENT",
                            IsReadAppNotification = false,
                            CreatedAt = DateTime.Now,
                            Status = "SENT",
                            RelatedRecordID = entityTablePrimaryKeyID,
                            RelatedRecordType = entityCode,
                            LogoText = notificationConfig.LogoText,
                            NotificationLink = notificationConfig.NotificationLink
                        });

                        await _dbContext.SaveChangesAsync();
                    }
                }
            }

        }

        public async Task<NotificationConfigDto> GetNotificationConfigForLevaeApplicationEntity(
      string EntityCode,
      int LevelNumber,
      string SenderName,
      string ReceiverName,
      string? rejectReason,
      string employeeName,
      string? leaveType,
      DateTime fromDate,
      DateTime toDate,
      decimal totalDays,
      string? reason,
      string? approvalWorkflow = null // use for #ApprovalWorkflow# in LEAVE_MULTILEVEL
  )
        {
            try
            {
                

                var nConfig = await _dbContext.NotificationsConfig
                    .Where(n => n.EntityCode == EntityCode)
                    .FirstOrDefaultAsync();

                if (nConfig == null || string.IsNullOrEmpty(nConfig.EmailContent))
                    return null;

                string fromDateStr = fromDate.ToString("dd-MMM-yyyy");
                string toDateStr = toDate.ToString("dd-MMM-yyyy");
                string totalDaysStr = totalDays.ToString("0.##");

                string approvalWorkflowText = approvalWorkflow ?? "";

                // Email content replacements
                string emailContent = nConfig.EmailContent
                    .Replace("#SENDER#", SenderName)
                    .Replace("#RECEIVER#", ReceiverName)
                    .Replace("#ApprovedBy", SenderName)
                    .Replace("#REJECTIONREASON#", rejectReason ?? "")
                    .Replace("#CURRENTDATETIME#", DateTime.Now.ToString("dd-MMM-yyyy hh:mm tt"))
                    .Replace("#ApprovalWorkflow#", approvalWorkflowText)

                    // Leave placeholders
                    .Replace("#EMPLOYEENAME#", employeeName ?? "")
                    .Replace("#LEAVETYPE#", leaveType ?? "")
                    .Replace("#FROMDATE#", fromDateStr)
                    .Replace("#TODATE#", toDateStr)
                    .Replace("#TOTALDAYS#", totalDaysStr)
                    .Replace("#REASON#", reason ?? "");

                // App content replacements
                string appContent = (nConfig.AppNotificationText ?? "")
                    .Replace("#SENDER#", SenderName)
                    .Replace("#RECEIVER#", ReceiverName)
                    .Replace("#APPROVERNAME#", SenderName)
                    .Replace("#REJECTIONREASON#", rejectReason ?? "")
                    .Replace("#CURRENTDATETIME#", DateTime.Now.ToString("dd-MMM-yyyy hh:mm tt"))
                    .Replace("#ApprovalWorkflow#", approvalWorkflowText)

                    // Leave placeholders
                    .Replace("#EMPLOYEENAME#", employeeName ?? "")
                    .Replace("#LEAVETYPE#", leaveType ?? "")
                    .Replace("#FROMDATE#", fromDateStr)
                    .Replace("#TODATE#", toDateStr)
                    .Replace("#TOTALDAYS#", totalDaysStr)
                    .Replace("#REASON#", reason ?? "");

                // Notification link (you said: simple)
                string notificationLink = "leave-applications";

                return new NotificationConfigDto
                {
                    IdNotificationConfig = nConfig.IdNotificationConfig,
                    NotificationType = nConfig.NotificationType,
                    EntityCode = nConfig.EntityCode,
                    EmailSubject = nConfig.EmailSubject,
                    EmailContent = emailContent,
                    LogoText = nConfig.LogoText,
                    NotificationLink = notificationLink,
                    AppNotificationText = appContent,
                    WebLink = nConfig.WebLink
                };
            }
            catch
            {
                throw;
            }
        }

        private static string BuildApprovalWorkflowHtml(string? approvalDetailsJson)
        {
            if (string.IsNullOrWhiteSpace(approvalDetailsJson))
                return "";

            try
            {
                var rows = JsonSerializer.Deserialize<List<LeaveApprovalRow>>(approvalDetailsJson,
                    new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

                if (rows == null || rows.Count == 0) return "";

                // sort by level
                rows = rows.OrderBy(r => r.Level).ToList();

                var sb = new StringBuilder();

                sb.AppendLine(@"
<table border='1' cellpadding='6' cellspacing='0' style='border-collapse:collapse;width:100%;font-family:Arial,sans-serif;font-size:13px;'>
  <thead>
    <tr>
      <th align='left'>Level</th>
      <th align='left'>Approver</th>
      <th align='left'>Status</th>
      <th align='left'>Status Date</th>
    </tr>
  </thead>
  <tbody>");

                foreach (var r in rows)
                {
                    var levelText = $"Level {r.Level}";
                    var approverText = string.IsNullOrWhiteSpace(r.Name) ? "-" : r.Name;
                    var statusText = string.IsNullOrWhiteSpace(r.Status) ? "PENDING" : r.Status;

                    string dateText = "-";
                    if (r.StatusDate.HasValue)
                        dateText = r.StatusDate.Value.ToString("dd-MM-yyyy");

                    sb.AppendLine($@"
    <tr>
      <td>{System.Net.WebUtility.HtmlEncode(levelText)}</td>
      <td>{System.Net.WebUtility.HtmlEncode(approverText)}</td>
      <td>{System.Net.WebUtility.HtmlEncode(statusText)}</td>
      <td>{System.Net.WebUtility.HtmlEncode(dateText)}</td>
    </tr>");
                }

                sb.AppendLine(@"
  </tbody>
</table>");

                return sb.ToString();
            }
            catch
            {
                // If JSON invalid, don't break email
                return "";
            }
        }

        private class LeaveApprovalRow
        {
            public int Level { get; set; }
            public string? Type { get; set; }
            public int? Id { get; set; }
            public string? Name { get; set; }
            public string? Status { get; set; }
            public DateTime? StatusDate { get; set; }
        }

        private async Task<List<Dictionary<string, string>>> GetEmployeesByIds(List<int> employeeIds)
        {
            if (employeeIds == null || employeeIds.Count == 0)
                return new List<Dictionary<string, string>>();

            var empDataList = await _dbContext.Employees
                .Where(e => employeeIds.Contains((int)e.IdEmployee))
                .Select(e => new
                {
                    FirstName = e.FirstName,
                    MiddleName = e.MiddleName,
                    LastName = e.LastName,
                    Email = e.EmailID
                })
                .ToListAsync();

            var result = empDataList
                .Select(e => new Dictionary<string, string>
                {
                    ["Name"] = $"{e.FirstName ?? ""} {e.MiddleName ?? ""} {e.LastName ?? ""}".Trim(),
                    ["Email"] = e.Email ?? ""
                })
                .ToList();

            return result;
        }

        public async Task<NotificationConfigDto> GetNotificationConfigForEntity(
     string EntityCode,
     int LevelNumber,
     string SenderName,
     string ReceiverName,
     string? rejectReason)
        {
            try
            {
                if (!string.IsNullOrEmpty(rejectReason))
                {
                    LevelNumber = 0;
                }
                
                var nConfig = await _dbContext.NotificationsConfig.Where(n => n.EntityCode == EntityCode && n.LevelNumber == LevelNumber).FirstOrDefaultAsync();

                if (nConfig == null || string.IsNullOrEmpty(nConfig.EmailContent))
                {
                    return null; // or new NotificationConfigDto() if you prefer
                }

                // Prepare processed content
                string emailContent = nConfig.EmailContent
                    .Replace("#SENDER#", SenderName)
                    .Replace("#RECEIVER#", ReceiverName)
                    .Replace("#ApprovedBy",SenderName)
                    .Replace("#REJECTIONREASON#", rejectReason ?? "")
                    .Replace("#CURRENTDATETIME#", DateTime.Now.ToString("dd-MMM-yyyy hh:mm tt"));

                string appContent = nConfig.AppNotificationText?
                    .Replace("#SENDER#", SenderName)
                    .Replace("#RECEIVER#", ReceiverName)
                       .Replace("#APPROVERNAME#", SenderName)
                    .Replace("#REJECTIONREASON#", rejectReason ?? "")
                    .Replace("#CURRENTDATETIME#", DateTime.Now.ToString("dd-MMM-yyyy hh:mm tt"));


                string notificationLink;

                // Case 1: Level = 1 → always "config-approvals"
                if (LevelNumber == 1)
                {
                    notificationLink = "config-approvals";
                }
                // Case 2: Level = 0 or 99 → depends on EntityCode
                else if (LevelNumber == 0 || LevelNumber == 99)
                {
                    switch (EntityCode)
                    {
                        case "SALTEM":
                            notificationLink = "salary-templates";
                            break;
                        case "EMPLSALCONFIG":
                            notificationLink = "employee-salary-config";
                            break;
                        case "OVERTIME":
                            notificationLink = "overtime-transactions";
                            break;
                        case "LEAVEPASS":
                            notificationLink = "leave-passages";
                            break;
                        default:
                            notificationLink = "config-approvals";
                            break;
                    }
                }
                // Default → config-approvals
                else
                {
                    notificationLink = "config-approvals";
                }

                // Return DTO (not EF entity)
                return new NotificationConfigDto
                {
                    IdNotificationConfig = nConfig.IdNotificationConfig,
                    NotificationType = nConfig.NotificationType,
                    EntityCode = nConfig.EntityCode,
                    EmailSubject = nConfig.EmailSubject,
                    EmailContent = emailContent,
                    LogoText = nConfig.LogoText,
                    NotificationLink = notificationLink,
                    AppNotificationText = appContent,
                    WebLink = nConfig.WebLink
                };
            }
            catch (Exception)
            {
                throw;
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






        /// <summary>
        /// Fetches target employees based on the approval authority type.
        /// </summary>
        private async Task<List<int>> GetTargetEmployees(WorkFlowConfigDetails workflowConfigDetails, int loggedInEmployeeId)
        {
            if (workflowConfigDetails == null)
                return new List<int> { 0 };

            List<int> result = new List<int>();

            if (workflowConfigDetails.ApprovalAuthorityType == "ROLE")
            {
                var designationId = workflowConfigDetails.ApprovalAuthorityID;
                if (designationId.HasValue)
                {
                    result = await _dbContext.Employees
                        .Where(e => e.IdDesignation == designationId.Value && e.IdEmployee.HasValue)
                        .Select(e => e.IdEmployee.Value)
                        .ToListAsync();
                }
            }
            else if (workflowConfigDetails.ApprovalAuthorityType == "REPOFFICER"
                     && workflowConfigDetails.ApprovalAuthorityID.HasValue)
            {
                result = await _dbContext.Employees
                    .Where(e => e.IdEmployee == loggedInEmployeeId && e.ReportingTo.HasValue)
                    .Select(e => e.ReportingTo.Value)
                    .ToListAsync();
            }
            else if (workflowConfigDetails.ApprovalAuthorityType == "EMPLOYEE")
            {
                if (workflowConfigDetails.ApprovalAuthorityID.HasValue)
                {
                    result = await _dbContext.Employees
                        .Where(e => e.IdEmployee == workflowConfigDetails.ApprovalAuthorityID.Value && e.IdEmployee.HasValue)
                        .Select(e => e.IdEmployee.Value)
                        .ToListAsync();
                }
            }

            // If result is null or empty, append 0
            if (result == null || !result.Any())
                result = new List<int> { 0 };

            return result;
        }


        public async Task<IEnumerable<ConfigApprovalsDto>> GetConfigApprovalsList(DateTime fromDate, string? actionStatus = null, string? entityCode = null, string? targetIdEmployee = null)
        {


            try
            {
                using (var connection = _dbContext.Database.GetDbConnection() as SqlConnection)
                {
                    var parameters = new DynamicParameters();
                    parameters.Add("@IdApprover", targetIdEmployee, DbType.Int32);
                    parameters.Add("@DateFrom", fromDate, DbType.DateTime);
                    parameters.Add("@EntityCode", entityCode, DbType.String);
                    parameters.Add("@ActionStatus", actionStatus, DbType.String);

                    var result = await connection.QueryAsync<ConfigApprovalsDto>(
                        "GetDataForConfigApproval_NEW",
                        parameters,
                        commandType: CommandType.StoredProcedure);

                    return result.ToList();
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
