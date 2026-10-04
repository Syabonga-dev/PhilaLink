using Microsoft.EntityFrameworkCore;
using PersonalProject.Data;

namespace PersonalProject.Tests.Support
{
    internal static class TestDb
    {
        public static PhilaLinkDbContext
            Create()
        {
            var options =
                new DbContextOptionsBuilder<
                    PhilaLinkDbContext
                >()
                .UseInMemoryDatabase(
                    $"philalink-tests-{Guid.NewGuid():N}"
                )
                .Options;

            var context =
                new PhilaLinkDbContext(
                    options
                );

            context.Database
                .EnsureCreated();

            return context;
        }
    }
}
