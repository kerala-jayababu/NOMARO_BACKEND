using Georgetown_International_Academy.API.Database;
using Georgetown_Internationsl_Academy.API.Models;
using Georgetown_Internationsl_Academy.API.Services.Interface;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.Linq;
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
                    .Where(a => a.EntityTablePrimaryKeyID == entityTablePrimaryKeyID)
                    .OrderByDescending(a => a.LevelNumber)
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

                    var newRecord = new ApprovalWorkFlowAllocation
                    {
                        IdWorkFlowConfig = workflowConfig.IdWorkFlowConfig,
                        EntityCode = entityCode,
                        EntityTablePrimaryKeyID = entityTablePrimaryKeyID,
                        CycleIndex = (currentRecord != null) ? currentRecord.CycleIndex + 1 : 1,
                        LevelNumber = 1,
                        SourceIdEmployee = loggedInEmployeeId,
                        TargetIdEmployee = targetEmployeeIds,
                        ActionStatus = "SUBMITTED",
                        SentDate = DateTime.Now
                    };

                    await _dbContext.ApprovalWorkFlowAllocations.AddAsync(newRecord);
                    await _dbContext.SaveChangesAsync();
                  

                    return "Approval workflow initiated.";
                }

                // Step 3: Validate logged-in employee
                if (currentRecord.TargetIdEmployee == null ||
                    !currentRecord.TargetIdEmployee.Split(',').Contains(loggedInEmployeeId.ToString()))
                {
                    return "You are not authorized to approve/reject this record.";
                }


             



                // Step 4: Update current level's status
                currentRecord.ActionStatus = status;
                currentRecord.ActionDate = DateTime.Now;
                currentRecord.RejectionRemarks = status == "REJECTED" ? rejectReason : null;
                await _dbContext.SaveChangesAsync();

                // Step 5: Handle rejection
                if (status == "REJECTED")
                {
                    await UpdateEntityStatus(entityTablePrimaryKeyID, entityCode, "REJECTED", currentRecord.CycleIndex);
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
                    await UpdateEntityStatus(entityTablePrimaryKeyID, entityCode, "APPROVED", currentRecord.CycleIndex);
                    return "Record approved successfully. Workflow completed.";
                }

                // Step 7: Insert the next level record
                var targetEmployeesForNextLevel = await GetTargetEmployees(workflowConfigDetails);

                if (!targetEmployeesForNextLevel.Any())
                {
                    return "No target employees found for the next level.";
                }

                var targetEmployeeIdsForNextLevel = string.Join(",", targetEmployeesForNextLevel);

                var newNextLevelRecord = new ApprovalWorkFlowAllocation
                {
                    IdWorkFlowConfig = currentRecord.IdWorkFlowConfig,
                    EntityCode = currentRecord.EntityCode,
                    EntityTablePrimaryKeyID = currentRecord.EntityTablePrimaryKeyID,
                    CycleIndex = currentRecord.CycleIndex,
                    LevelNumber = nextLevelNumber,
                    SourceIdEmployee = loggedInEmployeeId,
                    TargetIdEmployee = targetEmployeeIdsForNextLevel,                    
                    SentDate = DateTime.Now
                };

                await _dbContext.ApprovalWorkFlowAllocations.AddAsync(newNextLevelRecord);
                await UpdateEntityStatus(entityTablePrimaryKeyID, entityCode, "INTERIMAPPROVED", newNextLevelRecord.CycleIndex);
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



        private async Task UpdateEntityStatus(int entityTablePrimaryKeyID, string entityCode, string finalStatus,int cycleIndex)
        {
          //bool allApproved = await _dbContext.ApprovalWorkFlowAllocations
          //          .Where(a => a.EntityTablePrimaryKeyID == entityTablePrimaryKeyID && a.EntityCode == entityCode && a.CycleIndex == cycleIndex && a.ActionStatus !=null)
          //          .AllAsync(a => a.ActionStatus == "APPROVED");

          //      bool anySubmitted = await _dbContext.ApprovalWorkFlowAllocations
          //          .AnyAsync(a => a.EntityTablePrimaryKeyID == entityTablePrimaryKeyID && a.EntityCode == entityCode && a.CycleIndex == cycleIndex && a.ActionStatus == "SUBMITTED");

          //      if (allApproved)
          //      {
          //          finalStatus = "APPROVED";
          //      }
          //      else if (anySubmitted)
          //      {
          //          finalStatus = finalStatus == "REJECTED"? "REJECTED": "INTERIMAPPROVED";
          //      }
               
            

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

      

    }
}
