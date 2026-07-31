using Microsoft.EntityFrameworkCore;
using StudentImportDemo.Entity;

namespace StudentImportDemo.Data
{
    public class AppDbContext : DbContext
    {
        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
        {
        }
        public DbSet<Student> Students { get; set; }
        public DbSet<ImportJob> ImportJobs { get; set; }
        public DbSet<ImportRowResult> ImportRowResults { get; set; }
    }
}
