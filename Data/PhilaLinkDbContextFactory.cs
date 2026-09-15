using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.Extensions.Configuration;

namespace PersonalProject.Data
{
    public class PhilaLinkDbContextFactory :
        IDesignTimeDbContextFactory<PhilaLinkDbContext>
    {
        public PhilaLinkDbContext CreateDbContext(
            string[] args
        )
        {
            var configuration =
                new ConfigurationBuilder()
                    .SetBasePath(
                        Directory.GetCurrentDirectory()
                    )
                    .AddJsonFile(
                        "appsettings.json",
                        optional: true
                    )
                    .AddJsonFile(
                        "appsettings.Development.json",
                        optional: true
                    )
                    .AddUserSecrets<
                        PhilaLinkDbContextFactory
                    >(
                        optional: true
                    )
                    .AddEnvironmentVariables()
                    .Build();

            var connectionString =
                configuration.GetConnectionString(
                    "DefaultConnection"
                );

            if (
                string.IsNullOrWhiteSpace(
                    connectionString
                )
            )
            {
                throw new InvalidOperationException(
                    "ConnectionStrings:DefaultConnection " +
                    "is missing for EF Core design-time operations."
                );
            }

            var optionsBuilder =
                new DbContextOptionsBuilder<
                    PhilaLinkDbContext
                >();

            optionsBuilder.UseSqlServer(
                connectionString
            );

            return new PhilaLinkDbContext(
                optionsBuilder.Options
            );
        }
    }
}