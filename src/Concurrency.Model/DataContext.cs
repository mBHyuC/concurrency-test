namespace Concurrency.Model
{
    using Microsoft.EntityFrameworkCore;

    public abstract class DataContext : DbContext
    {
        public DbSet<Run> Runs { get; set; }

        public DbSet<WorkItem> WorkItems { get; set; }

        public static DataContext CreateSqlServerContext(string connectionString) => new SqlserverContext(connectionString);

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
          
            modelBuilder.Entity<WorkItem>().HasKey(t => t.Id);
            modelBuilder.Entity<WorkItem>().Property(t => t.State).IsRequired();
            modelBuilder.Entity<WorkItem>().Property(t => t.Timestamp).IsRowVersion();
            
            base.OnModelCreating(modelBuilder);

        }
    }
}
