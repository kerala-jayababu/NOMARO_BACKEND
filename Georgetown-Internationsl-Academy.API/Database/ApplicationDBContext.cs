using Georgetown_Internationsl_Academy.API.Models;
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
        
    }
}
