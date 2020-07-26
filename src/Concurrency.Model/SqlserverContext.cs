namespace Concurrency.Model
{
    using Microsoft.EntityFrameworkCore;
    using Microsoft.Extensions.Logging;
    using System.Net;

    internal class SqlserverContext : DataContext
    {
        private readonly string connectionString;

        private static ILoggerFactory InitLogger()
        {
            var lFactory = new LoggerFactory();
            lFactory.AddProvider(new SQLServerEventSourceLoggerProvider());
            return lFactory;
        }
        private static readonly ILoggerFactory LoggingFactory = InitLogger();

        public SqlserverContext()
        {
            this.connectionString = "Server=(localdb)\\MSSQLLocalDB;Database=ABCD";
        }

        public SqlserverContext(string connectionString)
        {
            this.connectionString = connectionString;
        }

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            optionsBuilder.UseLoggerFactory(LoggingFactory);
            optionsBuilder.UseSqlServer(this.connectionString);
            base.OnConfiguring(optionsBuilder);
        }
    }
}
