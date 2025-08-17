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
using System.Collections.Generic;
using System.Data;
using System.Drawing;
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

        public async Task<string> InitiateApprovalWorkflow(int entityTablePrimaryKeyID, string entityCode, int loggedInEmployeeId, string? status, decimal? LeavePassageAmount, string? rejectReason, int count=1)
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
                            LogoText = notificationConfig.LogoText
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
                    targetEmployeeIdsForNextLevel = entity.IdEmployee.ToString();
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
                            LogoText = notificationConfig.LogoText
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
                    targetEmployeeIdsForNextLevel = entity.IdEmployee.ToString();
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
                            LogoText = notificationConfig.LogoText
                        };

                        await _dbContext.Notifications.AddAsync(obj);
                        await _dbContext.SaveChangesAsync();
                    }
                }

            }
            else if (entityCode == _configuration["WorkflowEntityCodes:LEAVEPASS"])
            {
                var entity = await _dbContext.LeavePassages.FindAsync(entityTablePrimaryKeyID);
                var employeedetails = await _dbContext.Employees.Where(x => x.IdEmployee == entity.IdEmployee).FirstOrDefaultAsync();
                var employeename = string.Concat(employeedetails.FirstName, employeedetails.MiddleName, employeedetails.LastName);
                if (entity != null)
                {

                    if (nextLevelNumber ==99)
                    {
                        entity.LeavePassageAmount = LeavePassageAmount;
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
                            LogoText = notificationConfig.LogoText
                        };

                        await _dbContext.Notifications.AddAsync(obj);
                        await _dbContext.SaveChangesAsync();
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


                if (finalStatus == "SUBMITTED" && count == 1)
                {
                    var employeeIdList = ParseEmployeeIds(targetEmployeeIdsForNextLevel);


                    foreach (var empId in employeeIdList)
                    {
                        var approverName = await GetFullNameById(empId);
                        var creatorName = await GetFullNameById(loggedInEmployeeId);
                        string createdDateTime = DateTime.Now.ToString("dd-MMM-yyyy hh:mm tt");

                        var notificationConfig = await _dbContext.NotificationsConfig
                            .FirstOrDefaultAsync(x => x.EntityCode == "SALARYGEN" && x.NotificationType == "Salary generated and Submitted");
                        if (notificationConfig != null)
                        {
                            string emailBody = await GenerateEmailBodyForSalaryGenertaion(notificationConfig.EmailContent, empId, loggedInEmployeeId);

                            await EmailService.SendMail(approverName.Email, notificationConfig.EmailSubject, emailBody);

                            Notification notification = new Notification();
                            notification.IdNotificationConfig = notificationConfig.IdNotificationConfig;
                            notification.EmailContent = emailBody;
                            notification.EmailSubject = notificationConfig.EmailSubject;
                            notification.NotificationType= notificationConfig.NotificationType;
                            notification.Status = "SENT";
                            notification.CreatedAt = DateTime.UtcNow;
                            notification.SentByIdEmployee = loggedInEmployeeId;
                            notification.EmailSentStatus = "SENT";
                            notification.ReceivedByIdEmployee = empId;
                            _dbContext.Notifications.Add(notification);
                        }
                    }
                    await _dbContext.SaveChangesAsync();
                }

                if (finalStatus == "FM Approved" &&  count == 1 || finalStatus == "HR Approved" && count == 1)
                {
                    var employeeIdList = ParseEmployeeIds(targetEmployeeIdsForNextLevel);


                    foreach (var empId in employeeIdList)
                    {

                        var approverName = await GetFullNameById(empId);
                        var creatorName = await GetFullNameById(loggedInEmployeeId);
                        string createdDateTime = DateTime.Now.ToString("dd-MMM-yyyy hh:mm tt");

                        var notificationConfig = await _dbContext.NotificationsConfig
                            .FirstOrDefaultAsync(x => x.EntityCode == "SALARYGEN" && x.NotificationType == "Salary Approved");
                        if (notificationConfig != null)
                        {
                            string emailBody = await GenerateEmailBodyForSalaryGenertaionApproval(notificationConfig.EmailContent, empId, loggedInEmployeeId);

                            await EmailService.SendMail(approverName.Email, notificationConfig.EmailSubject, emailBody);
                            Notification notification = new Notification();
                            notification.IdNotificationConfig = notificationConfig.IdNotificationConfig;
                            notification.EmailContent = emailBody;
                            notification.EmailSubject = notificationConfig.EmailSubject;
                            notification.NotificationType = notificationConfig.NotificationType;
                            notification.Status = "SENT";
                            notification.CreatedAt = DateTime.UtcNow;
                            notification.SentByIdEmployee = loggedInEmployeeId;
                            notification.EmailSentStatus = "SENT";
                            notification.ReceivedByIdEmployee = empId;
                            _dbContext.Notifications.Add(notification);
                        }
                    }
                    await _dbContext.SaveChangesAsync();
                }


                if (finalStatus == "APPROVED" && count == 1)
                {                  

                    var approvalworkflow = await _dbContext.ApprovalWorkFlowAllocations.Where(x => x.EntityTablePrimaryKeyID == entityTablePrimaryKeyID &&
                                               x.EntityCode == "EMPSALGEN" &&
                                               x.CycleIndex == cycleIndex).ToListAsync();
                    var previousApprovalWorkflow = approvalworkflow.OrderByDescending(x => x.LevelNumber).Skip(1) .FirstOrDefault();
                    var notificationConfig = await _dbContext.NotificationsConfig
                           .FirstOrDefaultAsync(x => x.EntityCode == "SALARYGEN" && x.NotificationType == "Final Approved");

                    var creatorName = await GetFullNameById(previousApprovalWorkflow.SourceIdEmployee);
                    string emailBody = await GenerateEmailBodyForSalaryGenertaionFinalApproval(notificationConfig.EmailContent, (int)loggedInEmployeeId, previousApprovalWorkflow.SourceIdEmployee);

                    await EmailService.SendMail(creatorName.Email, notificationConfig.EmailSubject, emailBody);
                    Notification notification = new Notification();
                    notification.IdNotificationConfig = notificationConfig.IdNotificationConfig;
                    notification.EmailContent = emailBody;
                    notification.EmailSubject = notificationConfig.EmailSubject;
                    notification.NotificationType = notificationConfig.NotificationType;
                    notification.Status = "SENT";
                    notification.CreatedAt = DateTime.UtcNow;
                    notification.SentByIdEmployee = loggedInEmployeeId;
                    notification.EmailSentStatus = "SENT";
                    notification.ReceivedByIdEmployee = previousApprovalWorkflow.SourceIdEmployee;
                    _dbContext.Notifications.Add(notification);
                    await _dbContext.SaveChangesAsync();
                }
                if (finalStatus == "REJECTED")
                {
                    var bankremittance = await _dbContext.BankRemittance.Where(x => x.IdEmployeeSalary == entityTablePrimaryKeyID).ToListAsync();
                    if (bankremittance != null)
                    {
                        _dbContext.BankRemittance.RemoveRange(bankremittance);
                        await _dbContext.SaveChangesAsync();
                    }

                    if (count == 1)
                    {

                    
                    var approvalworkflow = await _dbContext.ApprovalWorkFlowAllocations
                     .FirstOrDefaultAsync(x => x.EntityTablePrimaryKeyID == entityTablePrimaryKeyID &&
                                               x.EntityCode == "EMPSALGEN" &&
                                               x.CycleIndex == cycleIndex);
                    var notificationConfig = await _dbContext.NotificationsConfig
                           .FirstOrDefaultAsync(x => x.EntityCode == "SALARYGEN" && x.NotificationType == "Salary Rejected");

                    var creatorName = await GetFullNameById(approvalworkflow.SourceIdEmployee);
                    string emailBody = await GenerateEmailBodyForSalaryGenertaionRejection(notificationConfig.EmailContent, (int)loggedInEmployeeId, approvalworkflow.SourceIdEmployee);

                    await EmailService.SendMail(creatorName.Email, notificationConfig.EmailSubject, emailBody);
                        Notification notification = new Notification();
                        notification.IdNotificationConfig = notificationConfig.IdNotificationConfig;
                        notification.EmailContent = emailBody;
                        notification.EmailSubject = notificationConfig.EmailSubject;
                        notification.NotificationType = notificationConfig.NotificationType;
                        notification.Status = "SENT";
                        notification.CreatedAt = DateTime.UtcNow;
                        notification.SentByIdEmployee = loggedInEmployeeId;
                        notification.EmailSentStatus = "SENT";
                        notification.ReceivedByIdEmployee = approvalworkflow.SourceIdEmployee;
                        _dbContext.Notifications.Add(notification);
                        await _dbContext.SaveChangesAsync();
                    }
                }

            }
        }



        private async Task<List<Dictionary<string, string>>> GetEmployeesByIds(List<int> employeeIds)
        {
            if (employeeIds == null || employeeIds.Count == 0)
                return new List<Dictionary<string, string>>();

            var empDataList = await _dbContext.Employees
                .Where(e => employeeIds.Contains(e.IdEmployee))
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

        public async Task<NotificationConfig> GetNotificationConfigForEntity(string EntityCode, int LevelNumber, string SenderName, string ReceiverName,string? rejectReason)
        {
            try
            {
                if(rejectReason != null && rejectReason!= string.Empty)
                {
                    LevelNumber = 0;
                }
                var nConfig = await _dbContext.NotificationsConfig
                    .FirstOrDefaultAsync(n => n.EntityCode == EntityCode && n.LevelNumber == LevelNumber);

                if (nConfig == null || string.IsNullOrEmpty(nConfig.EmailContent))
                {
                    return new NotificationConfig(); 
                }

                string emailContent = nConfig.EmailContent;

                nConfig.EmailContent = emailContent
                    .Replace("#SENDER#", SenderName)
                    .Replace("#RECEIVER#", ReceiverName)
                    .Replace("#REJECTIONREASON#",rejectReason)
                    .Replace("#CURRENTDATETIME#", DateTime.Now.ToString("dd-MMM-yyyy hh:mm tt"));

                string appContent = nConfig.AppNotificationText;

                nConfig.AppNotificationText = appContent
                    .Replace("#SENDER#", SenderName)
                    .Replace("#RECEIVER#", ReceiverName)
                    .Replace("#REJECTIONREASON#", rejectReason)
                    .Replace("#CURRENTDATETIME#", DateTime.Now.ToString("dd-MMM-yyyy hh:mm tt"));




                return nConfig;
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

        private string GenerateActionUrlForLeavePassageForAPPROVEDREjected(string token)
        {
            var baseUrl = _configuration["BaseURL"];
            return $"{baseUrl}/#/auth/leave-passages?tk={token}";
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

        private async Task<string> GenerateEmailBodyForEmployeeSalryConfig(string template, int approverId, int? creatorId, string EnmployeeName, string actionUrl)
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


        private async Task<string> GenerateEmailBodyForLeavePasage(string template, int approverId, int? creatorId, string EnmployeeName, string actionUrl)
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

        private async Task<string> GenerateEmailBodyForSalaryGenertaion(string template, int approverId, int? creatorId)
        {
            var approverName = await GetFullNameById(approverId);
            var creatorName = await GetFullNameById(creatorId);
            string createdDateTime = DateTime.Now.ToString("dd-MMM-yyyy hh:mm tt");

            string content = template
                .Replace("#EmployeeName#", approverName.FullName)
                .Replace("#SubmittedBy#", creatorName.FullName);

            return content;
        }


        private async Task<string> GenerateEmailBodyForSalaryGenertaionApproval(string template, int approverId, int? creatorId)
        {
            var approverName = await GetFullNameById(approverId);
            var creatorName = await GetFullNameById(creatorId);
            string createdDateTime = DateTime.Now.ToString("dd-MMM-yyyy hh:mm tt");

            string content = template
                .Replace("#EmployeeName#", approverName.FullName)
                .Replace("#ApprovedBy#", creatorName.FullName);

            return content;
        }

        private async Task<string> GenerateEmailBodyForSalaryGenertaionFinalApproval(string template, int approverId,int? creatorId)
        {
            var approverName = await GetFullNameById(approverId);
            //var creatorName = await GetFullNameById(creatorId);
            string createdDateTime = DateTime.Now.ToString("dd-MMM-yyyy hh:mm tt");

            string content = template
                .Replace("#ApprovedBy#", approverName.FullName);

            return content;
        }
        private async Task<string> GenerateEmailBodyForSalaryGenertaionRejection(string template, int approverId, int? creatorId)
        {
            var approverName = await GetFullNameById(approverId);
            var creatorName = await GetFullNameById(creatorId);
            string createdDateTime = DateTime.Now.ToString("dd-MMM-yyyy hh:mm tt");

            string content = template
                .Replace("#EmployeeName#", creatorName.FullName)
                .Replace("#RejectedBy#", approverName.FullName);

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



        private async Task<Notification> CreateNotificationforEnmployeeSalaryConfig(string approverName, string employeename, string creatorName, string createdDateTime, NotificationConfig notificationConfig, int loggedInEmployeeId, int empId, int entityTablePrimaryKeyID, string templateType, string? rejectRemarks)
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
                LogoText = notificationConfig.LogoText,
                Status = "SENT",
                RelatedRecordID = entityTablePrimaryKeyID,
                RelatedRecordType = "EMPSALCONFIG",
                CreatedAt = DateTime.Now
            };

        }

        private async Task<Notification> CreateNotificationforLeavePasage(string approverName, string employeename, string creatorName, string createdDateTime, NotificationConfig notificationConfig, int loggedInEmployeeId, int empId, int entityTablePrimaryKeyID, string templateType, string? rejectRemarks)
        {

            string appNotificationText = templateType switch
            {
                "SUBMITTED" => $"A Leave Passage submitted by {""} {creatorName} requires your action.",
                "APPROVED" => $"Leave Passage APPROVED.",
                "REJECTED" => $"Leave Passage has been REJECTED by  {""} {approverName}.",
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
            if (templateType == "APPROVED")
            {
                emailContent = emailContent
                    .Replace("#CREATORNAME#", creatorName)
                    .Replace("#APPROVERNAME#", approverName)
                    .Replace("#APPROVEDDATETIME#", createdDateTime);
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
      : "leave-passages";

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
                LogoText = notificationConfig.LogoText,
                Status = "SENT",
                RelatedRecordID = entityTablePrimaryKeyID,
                RelatedRecordType = "LEAVEPASS",
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
                LogoText = notificationConfig.LogoText,
                AppNotificationText = appNotificationText, // now generated
                NotificationLink = notificationLink,
                IsReadAppNotification = false,
                Status = "SENT",
                RelatedRecordID = entityTablePrimaryKeyID,
                RelatedRecordType = "OVERTIME",
                CreatedAt = DateTime.Now
            };

        }




        private async Task<Notification> CreateNotificationforSalaryTemplate(int fromId, int toId, NotificationConfig config, string approverName, string creatorName, int relatedId, string createdDateTime, string salaryTemplateName, string templateType, string? rejectRemarks)
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
            else if (templateType == "REJECTED")
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
                LogoText = config.LogoText,
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
        private async Task<List<int>> GetTargetEmployees(WorkFlowConfigDetails workflowConfigDetails, int loggedInEmployeeId)
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
                return await _dbContext.Employees
                    .Where(e => e.IdEmployee == loggedInEmployeeId)
                    .Select(e => (int)e.ReportingTo)
                    .ToListAsync();
            }
            else if (workflowConfigDetails.ApprovalAuthorityType == "EMPLOYEE")
            {
                return await _dbContext.Employees
                    .Where(e => e.IdEmployee == loggedInEmployeeId)
                    .Select(e => (int)e.IdEmployee)
                    .ToListAsync();
            }

            return new List<int>();
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
