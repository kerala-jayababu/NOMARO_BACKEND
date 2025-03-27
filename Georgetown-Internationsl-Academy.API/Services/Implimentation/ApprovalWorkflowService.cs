using Dapper;
using Georgetown_International_Academy.API.Database;
using Georgetown_Internationsl_Academy.API.DTO;
using Georgetown_Internationsl_Academy.API.Models;
using Georgetown_Internationsl_Academy.API.Services.Interface;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
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
        public ApprovalWorkflowService(ApplicationDBContext dbContext, IConfiguration configuration)
        {
            _dbContext = dbContext;
            _configuration = configuration;
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
                        await UpdateEntityStatus(entityTablePrimaryKeyID, entityCode, "FINAL APPROVED", 1, null);                        
                        return "Approval workflow initiated.";
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
                currentRecord.ActionStatus = status;
                currentRecord.ActionDate = DateTime.Now;
                currentRecord.RejectionRemarks = status == "REJECTED" ? rejectReason : null;
                await _dbContext.SaveChangesAsync();

                // Step 5: Handle rejection
                if (status == "REJECTED")
                {
                    await UpdateEntityStatus(entityTablePrimaryKeyID, entityCode, "REJECTED", currentRecord.CycleIndex,null);
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
                    await UpdateEntityStatus(entityTablePrimaryKeyID, entityCode, "APPROVED", currentRecord.CycleIndex, loggedInEmployeeId);
                    return "Record approved successfully. Workflow completed.";
                }

                // Step 7: Insert the next level record
                var targetEmployeesForNextLevel = await GetTargetEmployees(workflowConfigDetails);

                if (!targetEmployeesForNextLevel.Any())
                {
                    return "No target employees found for the next level.";
                }

                var targetEmployeeIdsForNextLevel = string.Join(",", targetEmployeesForNextLevel);
                string actionStatus = (targetEmployeeIdsForNextLevel == "0") ? "FINAL APPROVED" : "INTERIM APPROVED";
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
                await UpdateEntityStatus(entityTablePrimaryKeyID, entityCode, actionStatus, newNextLevelRecord.CycleIndex, loggedInEmployeeId);
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




        private async Task UpdateEntityStatus(int entityTablePrimaryKeyID, string entityCode, string finalStatus,int cycleIndex,int? loggedInEmployeeId)
        {
            

            if (entityCode == _configuration["WorkflowEntityCodes:SalaryTemplate"])
            {
                var entity = await _dbContext.SalaryTemplates.FindAsync(entityTablePrimaryKeyID);
                if (entity != null)
                {
                    entity.ApprovalStatus = finalStatus;
                    await _dbContext.SaveChangesAsync();
                }
            }
            else if (entityCode == _configuration["WorkflowEntityCodes:EmployeeSalaryConfig"])
            {
                var entity = await _dbContext.EmployeeSalaryConfig.FindAsync(entityTablePrimaryKeyID);
                if (entity != null)
                {
                    entity.ApprovalStatus = finalStatus;
                    await _dbContext.SaveChangesAsync();
                }
            }
            else if (entityCode == _configuration["WorkflowEntityCodes:OVERTIME"])
            {
                var entity = await _dbContext.OvertimeTransactions.FindAsync(entityTablePrimaryKeyID);
                if (entity != null)
                {
                    entity.ApprovalStatus = finalStatus;
                    await _dbContext.SaveChangesAsync();
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
                return new List<int> { workflowConfigDetails.ApprovalAuthorityID.Value }; // Convert nullable int to int safely
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
