using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Diagnostics;
using PersonalProject.Models.Entities;

namespace PersonalProject.Security
{
    /*
     * Automatically increments User.TokenVersion whenever
     * security-sensitive account information changes.
     *
     * This prevents individual services/controllers from
     * having to remember to revoke sessions manually.
     */
    public sealed class TokenVersionSaveChangesInterceptor :
        SaveChangesInterceptor
    {
        public override InterceptionResult<int>
            SavingChanges(
                DbContextEventData eventData,
                InterceptionResult<int> result
            )
        {
            AdvanceTokenVersions(
                eventData.Context
            );

            return base.SavingChanges(
                eventData,
                result
            );
        }

        public override ValueTask<
            InterceptionResult<int>
        > SavingChangesAsync(
            DbContextEventData eventData,
            InterceptionResult<int> result,
            CancellationToken cancellationToken =
                default
        )
        {
            AdvanceTokenVersions(
                eventData.Context
            );

            return base.SavingChangesAsync(
                eventData,
                result,
                cancellationToken
            );
        }

        private static void
            AdvanceTokenVersions(
                DbContext? context
            )
        {
            if (context == null)
            {
                return;
            }

            foreach (
                var entry in
                    context.ChangeTracker
                        .Entries<User>()
            )
            {
                if (
                    entry.State ==
                    EntityState.Added
                )
                {
                    if (
                        entry.Entity.TokenVersion <
                        1
                    )
                    {
                        entry.Entity.TokenVersion =
                            1;
                    }

                    continue;
                }

                if (
                    entry.State !=
                    EntityState.Modified
                )
                {
                    continue;
                }

                if (
                    entry.Property(
                        nameof(
                            User.PasswordHash
                        )
                    ).IsModified
                )
                {
                    entry.Entity
                        .ClearLoginAbuseState();

                    MarkLoginAbuseStateModified(
                        entry
                    );
                }

                if (
                    !HasSecuritySensitiveChange(
                        entry
                    )
                )
                {
                    continue;
                }

                entry.Entity.TokenVersion =
                    checked(
                        Math.Max(
                            1,
                            entry.Entity
                                .TokenVersion +
                            1
                        )
                    );
            }
        }

        private static void
            MarkLoginAbuseStateModified(
                EntityEntry<User> entry
            )
        {
            entry.Property(
                nameof(
                    User.FailedLoginAttempts
                )
            ).IsModified = true;

            entry.Property(
                nameof(
                    User.LastFailedLoginAtUtc
                )
            ).IsModified = true;

            entry.Property(
                nameof(
                    User.LockoutEndUtc
                )
            ).IsModified = true;
        }

        private static bool
            HasSecuritySensitiveChange(
                EntityEntry<User> entry
            )
        {
            return
                entry.Property(
                    nameof(
                        User.PasswordHash
                    )
                ).IsModified
                ||
                entry.Property(
                    nameof(
                        User.IsActive
                    )
                ).IsModified
                ||
                entry.Property(
                    nameof(
                        User.IsVerified
                    )
                ).IsModified
                ||
                entry.Property(
                    nameof(
                        User.MustChangePassword
                    )
                ).IsModified
                ||
                entry.Property(
                    nameof(
                        User.Role
                    )
                ).IsModified
                ||
                entry.Property(
                    nameof(
                        User.IdNumber
                    )
                ).IsModified
                ||
                entry.Property(
                    nameof(
                        User.Email
                    )
                ).IsModified
                ||
                entry.Property(
                    nameof(
                        User.PhoneNumber
                    )
                ).IsModified;
        }
    }
}