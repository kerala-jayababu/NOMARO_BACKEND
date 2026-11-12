using Nomaro.API.Models;
using Microsoft.EntityFrameworkCore;

namespace Nomaro.API.Database
{
    // Context for the NOMARO_GLOBAL database which holds the per-company tenant configuration.
    public class GlobalDbContext : DbContext
    {
        public GlobalDbContext(DbContextOptions<GlobalDbContext> options) : base(options)
        {
        }

        public DbSet<NomaroInstanceConfig> NomaroInstanceConfigs { get; set; }
        public DbSet<CompanyScreenAttributeConfig> CompanyScreenAttributeConfigs { get; set; }
        public DbSet<CompanyRegistration> CompanyRegistrations { get; set; }
        public DbSet<CompanyRegistrationOtp> CompanyRegistrationOtps { get; set; }
    }
}
