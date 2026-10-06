using Microsoft.EntityFrameworkCore;
using Npgsql;
using PersonalProject.Data;

namespace PersonalProject.Tests.Support
{
    /*
     * Creates one disposable PostgreSQL database per test.
     *
     * The supplied PHILALINK_TEST_POSTGRES connection is used
     * only as the administrative connection from which the
     * temporary database is created.
     *
     * The actual test runs against:
     *
     * philalink_test_<random-guid>
     *
     * and that database is dropped during disposal.
     */
    internal sealed class
        PostgreSqlTestDatabase :
        IAsyncDisposable
    {
        private readonly string
            _adminConnectionString;

        private bool
            _disposed;

        private PostgreSqlTestDatabase(
            string adminConnectionString,
            string databaseName,
            PhilaLinkDbContext context
        )
        {
            _adminConnectionString =
                adminConnectionString;

            DatabaseName =
                databaseName;

            Context =
                context;
        }

        public string DatabaseName
        {
            get;
        }

        public PhilaLinkDbContext Context
        {
            get;
        }

        public static async Task<
            PostgreSqlTestDatabase
        >
            CreateAsync()
        {
            var rawConnectionString =
                Environment
                    .GetEnvironmentVariable(
                        "PHILALINK_TEST_POSTGRES"
                    );

            if (
                string.IsNullOrWhiteSpace(
                    rawConnectionString
                )
            )
            {
                throw new InvalidOperationException(
                    "PHILALINK_TEST_POSTGRES has not been configured."
                );
            }

            var adminBuilder =
                new NpgsqlConnectionStringBuilder(
                    rawConnectionString
                );

            ValidateAdministrativeConnection(
                adminBuilder
            );

            /*
             * Do not pool administrative/test connections.
             *
             * Pooling can otherwise leave a connection attached
             * to the disposable database and prevent cleanup.
             */
            adminBuilder.Pooling =
                false;

            adminBuilder.ApplicationName =
                "PhilaLink.Tests";

            if (
                string.IsNullOrWhiteSpace(
                    adminBuilder.Database
                )
            )
            {
                adminBuilder.Database =
                    "postgres";
            }

            var databaseName =
                $"philalink_test_{Guid.NewGuid():N}";

            await CreateDatabaseAsync(
                adminBuilder.ConnectionString,
                databaseName
            );

            var testBuilder =
                new NpgsqlConnectionStringBuilder(
                    adminBuilder
                        .ConnectionString
                )
                {
                    Database =
                        databaseName,

                    Pooling =
                        false,

                    ApplicationName =
                        "PhilaLink.Tests"
                };

            var options =
                new DbContextOptionsBuilder<
                    PhilaLinkDbContext
                >()
                .UseNpgsql(
                    testBuilder
                        .ConnectionString
                )
                .EnableDetailedErrors()
                .Options;

            var context =
                new PhilaLinkDbContext(
                    options
                );

            try
            {
                /*
                 * This is deliberately Migrate rather than
                 * EnsureCreated.
                 *
                 * We need to prove that the same migration chain
                 * used in production can build a fresh PostgreSQL
                 * database successfully.
                 */
                await context.Database
                    .MigrateAsync();

                return new PostgreSqlTestDatabase(
                    adminBuilder
                        .ConnectionString,
                    databaseName,
                    context
                );
            }
            catch
            {
                await context
                    .DisposeAsync();

                await DropDatabaseAsync(
                    adminBuilder
                        .ConnectionString,
                    databaseName
                );

                throw;
            }
        }

        public async ValueTask
            DisposeAsync()
        {
            if (_disposed)
            {
                return;
            }

            _disposed =
                true;

            await Context
                .DisposeAsync();

            /*
             * Make sure Npgsql does not retain a connection to
             * the database being removed.
             */
            NpgsqlConnection
                .ClearAllPools();

            await DropDatabaseAsync(
                _adminConnectionString,
                DatabaseName
            );
        }

        private static void
            ValidateAdministrativeConnection(
                NpgsqlConnectionStringBuilder
                    builder
            )
        {
            if (
                string.IsNullOrWhiteSpace(
                    builder.Host
                )
            )
            {
                throw new InvalidOperationException(
                    "The PostgreSQL test connection must include a Host."
                );
            }

            /*
             * Safety guard.
             *
             * The administrative connection may point to the
             * standard PostgreSQL 'postgres' database or to a
             * database whose name explicitly contains 'test'.
             *
             * This makes it much harder to accidentally use a
             * production PhilaLink database for destructive
             * integration-test setup.
             */
            var database =
                builder.Database;

            if (
                !string.IsNullOrWhiteSpace(
                    database
                ) &&
                !database.Equals(
                    "postgres",
                    StringComparison
                        .OrdinalIgnoreCase
                ) &&
                !database.Contains(
                    "test",
                    StringComparison
                        .OrdinalIgnoreCase
                )
            )
            {
                throw new InvalidOperationException(
                    "PHILALINK_TEST_POSTGRES must point to the 'postgres' database or a database whose name contains 'test'. Never use the production PhilaLink database."
                );
            }
        }

        private static async Task
            CreateDatabaseAsync(
                string adminConnectionString,
                string databaseName
            )
        {
            await using var connection =
                new NpgsqlConnection(
                    adminConnectionString
                );

            await connection
                .OpenAsync();

            /*
             * databaseName is generated entirely inside the test
             * harness and contains only letters, numbers and
             * underscores.
             */
            var commandText =
                $"CREATE DATABASE \"{databaseName}\";";

            await using var command =
                new NpgsqlCommand(
                    commandText,
                    connection
                );

            await command
                .ExecuteNonQueryAsync();
        }

        private static async Task
            DropDatabaseAsync(
                string adminConnectionString,
                string databaseName
            )
        {
            await using var connection =
                new NpgsqlConnection(
                    adminConnectionString
                );

            await connection
                .OpenAsync();

            /*
             * End any remaining sessions before dropping the
             * disposable test database.
             */
            const string terminateSql =
                """
                SELECT pg_terminate_backend(pid)
                FROM pg_stat_activity
                WHERE datname = @databaseName
                  AND pid <> pg_backend_pid();
                """;

            await using (
                var terminate =
                    new NpgsqlCommand(
                        terminateSql,
                        connection
                    )
            )
            {
                terminate.Parameters
                    .AddWithValue(
                        "databaseName",
                        databaseName
                    );

                await terminate
                    .ExecuteNonQueryAsync();
            }

            var dropSql =
                $"DROP DATABASE IF EXISTS \"{databaseName}\";";

            await using var drop =
                new NpgsqlCommand(
                    dropSql,
                    connection
                );

            await drop
                .ExecuteNonQueryAsync();
        }
    }
}
