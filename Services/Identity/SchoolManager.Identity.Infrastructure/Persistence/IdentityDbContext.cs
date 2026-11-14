using Microsoft.EntityFrameworkCore;
using SchoolManager.Identity.Domain;

namespace SchoolManager.Identity.Infrastructure
{
    public class IdentityDbContext : DbContext
    {
        public IdentityDbContext(DbContextOptions<IdentityDbContext> options) : base(options)
        {
        }

        public DbSet<User> Users => Set<User>();
        public DbSet<Role> Roles => Set<Role>();
        public DbSet<UserProfile> UserProfiles => Set<UserProfile>();
        public DbSet<EmployeeGroup> EmployeeGroups => Set<EmployeeGroup>();
        public DbSet<Employee> Employees => Set<Employee>();
        public DbSet<MenuItem> MenuItems => Set<MenuItem>();

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.ApplyConfigurationsFromAssembly(typeof(IdentityDbContext).Assembly);
            base.OnModelCreating(modelBuilder);
        }
    }
}
