using Microsoft.EntityFrameworkCore;
using DeviceManagementAPI.Entities;

namespace DeviceManagementAPI.Data
{
    public class DeviceDbContext : DbContext
    {
        public DeviceDbContext(DbContextOptions<DeviceDbContext> options) : base(options) { }

        public DbSet<Device> Devices { get; set; }

        public DbSet<User> Users { get; set; }
       
        // 👇 Add this new DbSet
        public DbSet<RefreshToken> RefreshTokens { get; set; }
        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<Device>().HasQueryFilter(d => !d.IsDeleted);
            base.OnModelCreating(modelBuilder);
        }
        public DbSet<Notification> Notifications { get; set; }


    }
}
