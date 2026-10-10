using Microsoft.EntityFrameworkCore;
using PersonalProject.Data;
using PersonalProject.Services.Interfaces;

namespace PersonalProject.Services.Implementations
{
    public class SessionService :
        ISessionService
    {
        private readonly PhilaLinkDbContext
            _context;

        public SessionService(
            PhilaLinkDbContext context
        )
        {
            _context =
                context;
        }

        // =====================================================
        // REVOKE ALL CURRENT JWT SESSIONS
        // =====================================================

        public async Task
            RevokeAllSessionsAsync(
                Guid userId
            )
        {
            var user =
                await _context.Users
                    .FirstOrDefaultAsync(
                        item =>
                            item.Id ==
                            userId
                    );

            if (
                user == null ||
                !user.IsActive
            )
            {
                throw new UnauthorizedAccessException(
                    "Account is not available."
                );
            }

            /*
             * Explicit revocation increments TokenVersion
             * directly.
             *
             * The interceptor intentionally does not watch
             * TokenVersion itself, preventing a double
             * increment here.
             */
            user.TokenVersion =
                checked(
                    Math.Max(
                        1,
                        user.TokenVersion +
                        1
                    )
                );

            user.UpdatedAt =
                DateTime.UtcNow;

            await _context
                .SaveChangesAsync();
        }
    }
}