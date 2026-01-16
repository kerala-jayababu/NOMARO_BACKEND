using Georgetown_Internationsl_Academy.API.DTO;
using Georgetown_Internationsl_Academy.API.Models;
using Georgetown_Internationsl_Academy.API.Models.Shift;
using Georgetown_Internationsl_Academy.API.Models.Time___Attendance.Bamboo_HR;
using Georgetown_Internationsl_Academy.API.Models.Time___Attendance.Shift;
using Georgetown_Internationsl_Academy.API.Models.YourNamespace.Models;
using Georgetown_Internationsl_Academy.API.Services.Implimentation.Time___Attendance.BambooHR;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace Georgetown_International_Academy.API.Database
{
    public class ApplicationDBContext : DbContext
    {
        public ApplicationDBContext(DbContextOptions<ApplicationDBContext> options) : base(options)
        {
        }

        public DbSet<BudgetCodeEntity> BudgetCodes { get; set; }
        public DbSet<DesignationEntity> Designations { get; set; }
        public DbSet<DepartmentEntity> Departments { get; set; }
        public DbSet<SalaryHeads> SalaryHeads { get; set; }
        public DbSet<Employee> Employees { get; set; }
        public DbSet<Banks> Banks { get; set; }
        public DbSet<EmployeeBankAccount> EmployeeBankAccounts { get; set; }
        public DbSet<EmployeeOvertimeConfig> EmployeeOvertimeConfig { get; set; }
        public DbSet<BankBranches> BankBranches { get; set; }
        public DbSet<PayrollScreens> PayrollScreens { get; set; }
        public DbSet<EmployeePermissions> EmployeePermissions { get; set; }
        public DbSet<RoleBasedPermission> RoleBasedPermissions { get; set; }
        public DbSet<SystemParameter> SystemParameters { get; set; }
        public DbSet<NotificationConfig> NotificationsConfig { get; set; }
        public DbSet<VacationMode> VacationModes { get; set; }
        public DbSet<TaxSlab> TaxSlabs { get; set; }
        public DbSet<FinancialYears> FinancialYears { get; set; }        
        public DbSet<CurrencyConversion> CurrencyConversions { get; set; }
        public DbSet<ChildTaxThreshold> ChildTaxThresholds { get; set; }
        public DbSet<SalaryTemplate> SalaryTemplates { get; set; }
        public DbSet<SalaryTemplateDetails> SalaryTemplateDetails { get; set; }
        public DbSet<OvertimeTransactionEntity> OvertimeTransactions { get; set; }
        public DbSet<SalaryAdjustment> SalaryAdjustments { get; set; }
        public DbSet<ScheduledSalaryDeduction> ScheduledDeductions { get; set; }
        public DbSet<ScheduledDeductionDetails> ScheduledDeductionDetails { get; set; }
        public DbSet<MaternityLeaveSalaryEntity> MaternityLeaveSalaries { get; set; }
        public DbSet<RentFreeQuarter> RentFreeQuarters { get; set; }
        public DbSet<OvertimeTypes> OvertimeTypes { get; set; }
        public DbSet<EmployeeSalaryConfigDetails> EmployeeSalaryConfigDetails { get; set; }
        public DbSet<EmployeeSalaryConfig> EmployeeSalaryConfig { get; set; }
        public DbSet<SalaryMonths> SalaryMonths { get; set; }
        public DbSet<MaternityLeaveSalaryDetail> MaternityLeaveSalaryDetail { get; set; }
        public DbSet<HolidayTypeEntity> HolidayTypes { get; set; }
        public DbSet<Holiday> Holidays { get; set; }
        public DbSet<WorkFlowConfig> WorkFlowConfig { get; set; }
        public DbSet<WorkFlowConfigDetails> WorkFlowConfigDetails { get; set; }
        public DbSet<ApprovalWorkFlowAllocation> ApprovalWorkFlowAllocations { get; set; }
        public DbSet<EmployeeSalaries> EmployeeSalaries { get; set; }
        public DbSet<RentFreeQuarterDurations> RentFreeQuarterDurations { get; set; }
        public DbSet<EmployeeSalaryDetails> EmployeeSalaryDetails { get; set; }
        public DbSet<LeavePassage> LeavePassages { get; set; }
        public DbSet<ReportsMaster> ReportsMaster { get; set; }
        public DbSet<ReportConditions> ReportConditions { get; set; }
        public DbSet<ReportColumns> ReportColumns { get; set; }

        public DbSet<Notification> Notifications { get; set; }
        public DbSet<LoginOTP> LoginOTP { get; set; }
        public DbSet<BankRemittance> BankRemittance { get; set; }
        public DbSet<BambooHRIntegrationLogs> BambooHRIntegrationLogs { get; set; }
        public DbSet<LeavePassageAmounts> LeavePassageAmounts { get; set; }
        public DbSet<RentFreeQuarterAllowance> RentFreeQuarterAllowance { get; set; }

        public DbSet<AssetTypes> AssetTypes { get; set; }
        public DbSet<Assets> Assets { get; set; }
        public DbSet<AssetAssignments> AssetAssignments { get; set; }
        public DbSet<EmployeeQualifications> EmployeeQualifications { get; set; }
        public DbSet<EmployeeExperiences> EmployeeExperiences { get; set; }
        public DbSet<QualificationTypes> QualificationTypes { get; set; }
        public DbSet<EmployeeActions> EmployeeActions { get; set; }
        public DbSet<Countries> Countries { get; set; }
        public DbSet<EmployeeDocuments> EmployeeDocuments { get; set; }
        public DbSet<DocumentTypes> DocumentTypes { get; set; }
        


        #region Time & Attendance
        public DbSet<ShiftDefinitionEntity> ShiftDefinitions { get; set; }
        public DbSet<ShiftSchedule> ShiftSchedules { get; set; }
        public DbSet<ShiftEmployee> ShiftEmployees { get; set; }
        public DbSet<ShiftAssignment> ShiftAssignments { get; set; }
        public DbSet<DayAttendance> DayAttendance { get; set; }
        public DbSet<ClockInOutDetails> ClockInOutDetails { get; set; }
        public DbSet<EmployeeUnauthorizedAbsence> EmployeeUnAuthorizedAbsence { get; set; }

        #endregion

        #region
        // Leave Master
        public DbSet<LeaveTypes> LeaveTypes { get; set; }

        // Annual Leave Configuration
        public DbSet<AnnualLeaveTypeConfig> AnnualLeaveTypeConfig { get; set; }

        // Leave Templates
        public DbSet<LeaveTemplates> LeaveTemplates { get; set; }
        public DbSet<LeaveTemplateDetails> LeaveTemplateDetails { get; set; }

        // Employee Leave Configuration
        public DbSet<EmployeeLeaveConfigs> EmployeeLeaveConfigs { get; set; }
        public DbSet<EmployeeLeaveConfigDetails> EmployeeLeaveConfigDetails { get; set; }

        // Leave Applications
        public DbSet<LeaveApplications> LeaveApplications { get; set; }
        public DbSet<LeaveApplicationDocuments> LeaveApplicationDocuments { get; set; }
        public DbSet<EmployeeServiceChanges> EmployeeServiceChanges { get; set; }

        public DbSet<ExitReasons> ExitReasons { get; set; }
        public DbSet<ExitTypes> ExitTypes { get; set; }
        public DbSet<NoticePeriodPolicies> NoticePeriodPolicies { get; set; }
        public DbSet<ClearanceTemplates> ClearanceTemplates { get; set; }

        #endregion

        #region BambooHR
        public DbSet<EmployeeLeave> EmployeeLeaves { get; set; }
        public DbSet<EmployeeLeaveDetail> EmployeeLeaveDetails { get; set; }
        public DbSet<BambooHRLeaveIntegrationLastRun> BambooHRLeaveIntegrationLastRun { get; set; }


        #endregion
      
    }
}
