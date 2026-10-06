using Xunit;

namespace PersonalProject.Tests.Support
{
    /*
     * PostgreSQL integration tests are intentionally separate
     * from the normal fast regression suite.
     *
     * Set:
     *
     * PHILALINK_TEST_POSTGRES
     *
     * to enable them.
     *
     * Example:
     *
     * Host=localhost;
     * Port=5432;
     * Database=postgres;
     * Username=postgres;
     * Password=...
     *
     * Never point this setting at the production database.
     */
    internal sealed class
        PostgreSqlFactAttribute :
        FactAttribute
    {
        public PostgreSqlFactAttribute()
        {
            var connectionString =
                Environment
                    .GetEnvironmentVariable(
                        "PHILALINK_TEST_POSTGRES"
                    );

            if (
                string.IsNullOrWhiteSpace(
                    connectionString
                )
            )
            {
                Skip =
                    "PostgreSQL integration test skipped. " +
                    "Set PHILALINK_TEST_POSTGRES to a dedicated PostgreSQL test/admin connection.";
            }
        }
    }
}
