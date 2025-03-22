using Georgetown_Internationsl_Academy.API.DTO;
using Georgetown_Internationsl_Academy.API.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using YourNamespace.Models;
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

        
    }
}
