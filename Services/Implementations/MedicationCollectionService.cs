using Microsoft.EntityFrameworkCore;
using PersonalProject.Data;
using PersonalProject.Models.Constants;
using PersonalProject.Models.DTOs;
using PersonalProject.Models.Entities;
using PersonalProject.Services.Interfaces;

namespace PersonalProject.Services.Implementations
{
    public class MedicationCollectionService :
        IMedicationCollectionService
    {
        private readonly PhilaLinkDbContext _context;
        private readonly IAuditLogService _audit;

        public MedicationCollectionService(
            PhilaLinkDbContext context,
            IAuditLogService audit
        )
        {
            _context = context;
            _audit = audit;
        }

        public async Task<MedicationCollectionResponseDto>
            CreateAsync(
                CreateMedicationCollectionDto dto,
                Guid performedByUserId
            )
        {
            var clinicId =
                await GetStaffClinicIdAsync(
                    performedByUserId
                );

            if (dto.ClinicId != clinicId)
            {
                throw new UnauthorizedAccessException(
                    "You can only schedule collections for your clinic."
                );
            }

            if (dto.Items.Count == 0)
            {
                throw new InvalidOperationException(
                    "A collection must contain at least one medication."
                );
            }

            var patient =
                await _context.Patients
                    .Include(p => p.User)
                    .FirstOrDefaultAsync(
                        p => p.Id == dto.PatientId
                    );

            if (patient == null)
            {
                throw new KeyNotFoundException(
                    "Patient not found."
                );
            }

            if (patient.ClinicId != clinicId)
            {
                throw new UnauthorizedAccessException(
                    "Patient does not belong to your clinic."
                );
            }

            var collection =
                new MedicationCollection
                {
                    Id = Guid.NewGuid(),

                    PatientId =
                        dto.PatientId,

                    ClinicId =
                        clinicId,

                    ScheduledCollectionDate =
                        dto.ScheduledCollectionDate,

                    Status =
                        "Scheduled",

                    Notes =
                        dto.Notes,

                    CreatedAt =
                        DateTime.UtcNow
                };

            foreach (var itemDto in dto.Items)
            {
                if (itemDto.Quantity <= 0)
                {
                    throw new InvalidOperationException(
                        "Collection quantity must be greater than zero."
                    );
                }

                var medication =
                    await _context.Medications
                        .FirstOrDefaultAsync(
                            m =>
                                m.Id ==
                                    itemDto.MedicationId &&
                                m.PatientId ==
                                    patient.Id &&
                                m.IsActive
                        );

                if (medication == null)
                {
                    throw new InvalidOperationException(
                        "Medication is not an active medication for this patient."
                    );
                }

                var stock =
                    await _context.ClinicStocks
                        .FirstOrDefaultAsync(
                            s =>
                                s.Id ==
                                    itemDto.ClinicStockId &&
                                s.ClinicId ==
                                    clinicId &&
                                s.IsActive
                        );

                if (stock == null)
                {
                    throw new InvalidOperationException(
                        "Selected clinic stock item was not found or is inactive."
                    );
                }

                if (
                    !string.Equals(
                        stock.MedicationName.Trim(),
                        medication.Name.Trim(),
                        StringComparison.OrdinalIgnoreCase
                    )
                )
                {
                    throw new InvalidOperationException(
                        "Selected clinic stock does not match the patient's medication."
                    );
                }

                collection.Items.Add(
                    new MedicationCollectionItem
                    {
                        Id = Guid.NewGuid(),

                        MedicationId =
                            medication.Id,

                        ClinicStockId =
                            stock.Id,

                        Quantity =
                            itemDto.Quantity,

                        Notes =
                            itemDto.Notes,

                        CreatedAt =
                            DateTime.UtcNow
                    }
                );
            }

            _context.MedicationCollections.Add(
                collection
            );

            await _context.SaveChangesAsync();

            await _audit.LogAsync(
                "MedicationCollectionCreated",
                performedByUserId,
                $"Collection {collection.Id} scheduled " +
                $"for patient {patient.Id}."
            );

            return await GetDtoAsync(
                collection.Id
            );
        }

        public async Task<
            List<MedicationCollectionResponseDto>>
            GetClinicCollectionsAsync(
                Guid performedByUserId
            )
        {
            var clinicId =
                await GetStaffClinicIdAsync(
                    performedByUserId
                );

            var collections =
                await GetCollectionQuery()
                    .Where(
                        c => c.ClinicId == clinicId
                    )
                    .OrderBy(
                        c => c.ScheduledCollectionDate
                    )
                    .ToListAsync();

            return collections
                .Select(ToDto)
                .ToList();
        }

        public async Task<MedicationCollectionSummaryDto>
            GetSummaryAsync(
                Guid performedByUserId
            )
        {
            var clinicId =
                await GetStaffClinicIdAsync(
                    performedByUserId
                );

            var today = DateTime.UtcNow.Date;
            var tomorrow = today.AddDays(1);

            var dayOfWeek =
                (int)today.DayOfWeek;

            var daysSinceMonday =
                dayOfWeek == 0
                    ? 6
                    : dayOfWeek - 1;

            var monday =
                today.AddDays(
                    -daysSinceMonday
                );

            var dueToday =
                await _context
                    .MedicationCollections
                    .CountAsync(c =>
                        c.ClinicId == clinicId &&
                        c.Status != "Collected" &&
                        c.Status != "Cancelled" &&
                        c.ScheduledCollectionDate >=
                            today &&
                        c.ScheduledCollectionDate <
                            tomorrow
                    );

            var overdue =
                await _context
                    .MedicationCollections
                    .CountAsync(c =>
                        c.ClinicId == clinicId &&
                        c.Status != "Collected" &&
                        c.Status != "Cancelled" &&
                        c.ScheduledCollectionDate <
                            today
                    );

            var collectedThisWeek =
                await _context
                    .MedicationCollections
                    .CountAsync(c =>
                        c.ClinicId == clinicId &&
                        c.Status == "Collected" &&
                        c.CollectedAt != null &&
                        c.CollectedAt >= monday
                    );

            var totalActive =
                await _context.Medications
                    .CountAsync(m =>
                        m.IsActive &&
                        m.Patient.ClinicId ==
                            clinicId
                    );

            return new MedicationCollectionSummaryDto
            {
                DueToday = dueToday,

                Overdue = overdue,

                CollectedThisWeek =
                    collectedThisWeek,

                TotalActive =
                    totalActive
            };
        }

        public async Task<MedicationCollectionResponseDto>
            AssignProxyAsync(
                Guid collectionId,
                Guid proxyId,
                Guid performedByUserId
            )
        {
            var clinicId =
                await GetStaffClinicIdAsync(
                    performedByUserId
                );

            var collection =
                await _context
                    .MedicationCollections
                    .FirstOrDefaultAsync(
                        c =>
                            c.Id == collectionId &&
                            c.ClinicId == clinicId
                    );

            if (collection == null)
            {
                throw new KeyNotFoundException(
                    "Collection not found."
                );
            }

            await ValidateProxyAssignmentAsync(
                collection.PatientId,
                proxyId
            );

            collection.ProxyId = proxyId;
            collection.UpdatedAt =
                DateTime.UtcNow;

            await _context.SaveChangesAsync();

            await _audit.LogAsync(
                "CollectionProxyAssigned",
                performedByUserId,
                $"Proxy {proxyId} assigned to " +
                $"collection {collection.Id}."
            );

            return await GetDtoAsync(
                collection.Id
            );
        }

        public async Task<MedicationCollectionResponseDto>
            CompleteAsync(
                Guid collectionId,
                CompleteMedicationCollectionDto dto,
                Guid performedByUserId
            )
        {
            var nurse =
                await GetActiveNurseAsync(
                    performedByUserId
                );

            await using var transaction =
                await _context.Database
                    .BeginTransactionAsync();

            var collection =
                await GetCollectionQuery()
                    .FirstOrDefaultAsync(
                        c =>
                            c.Id == collectionId &&
                            c.ClinicId ==
                                nurse.ClinicId
                    );

            if (collection == null)
            {
                throw new KeyNotFoundException(
                    "Collection not found."
                );
            }

            if (collection.Status == "Collected")
            {
                throw new InvalidOperationException(
                    "Collection has already been completed."
                );
            }

            if (collection.Status == "Cancelled")
            {
                throw new InvalidOperationException(
                    "Cancelled collection cannot be completed."
                );
            }

            var proxyId =
                dto.ProxyId ??
                collection.ProxyId;

            if (proxyId != null)
            {
                await ValidateProxyAssignmentAsync(
                    collection.PatientId,
                    proxyId.Value
                );
            }

            foreach (var item in collection.Items)
            {
                var stock =
                    item.ClinicStock;

                if (!stock.IsActive)
                {
                    throw new InvalidOperationException(
                        $"{stock.MedicationName} stock is inactive."
                    );
                }

                if (
                    stock.QuantityOnHand <
                    item.Quantity
                )
                {
                    throw new InvalidOperationException(
                        $"Insufficient stock for {stock.MedicationName}."
                    );
                }

                stock.QuantityOnHand -=
                    item.Quantity;

                stock.UpdatedAt =
                    DateTime.UtcNow;
            }

            collection.ProxyId =
                proxyId;

            collection.ProcessedByNurseId =
                nurse.Id;

            collection.CollectedAt =
                DateTime.UtcNow;

            collection.Status =
                "Collected";

            if (!string.IsNullOrWhiteSpace(dto.Notes))
            {
                collection.Notes =
                    string.IsNullOrWhiteSpace(
                        collection.Notes
                    )
                        ? dto.Notes
                        : $"{collection.Notes}\n{dto.Notes}";
            }

            collection.UpdatedAt =
                DateTime.UtcNow;

            await _context.SaveChangesAsync();

            await _audit.LogAsync(
                "MedicationCollectionCompleted",
                performedByUserId,
                $"Collection {collection.Id} completed."
            );

            await transaction.CommitAsync();

            return await GetDtoAsync(
                collection.Id
            );
        }

        private async Task ValidateProxyAssignmentAsync(
            Guid patientId,
            Guid proxyId
        )
        {
            var valid =
                await _context.ProxyLinks
                    .AnyAsync(pl =>
                        pl.PatientId ==
                            patientId &&
                        pl.ProxyId ==
                            proxyId &&
                        pl.Proxy.User.IsActive
                    );

            if (!valid)
            {
                throw new UnauthorizedAccessException(
                    "Proxy is not actively assigned to this patient."
                );
            }
        }

        private async Task<Nurse>
            GetActiveNurseAsync(
                Guid userId
            )
        {
            var nurse =
                await _context.Nurses
                    .Include(n => n.User)
                    .FirstOrDefaultAsync(
                        n =>
                            n.UserId == userId &&
                            n.User.Role ==
                                RoleNames.Nurse &&
                            n.User.IsActive
                    );

            if (nurse == null)
            {
                throw new UnauthorizedAccessException(
                    "An active Nurse account is required."
                );
            }

            return nurse;
        }

        private async Task<Guid>
            GetStaffClinicIdAsync(
                Guid userId
            )
        {
            var user =
                await _context.Users
                    .Include(u => u.Admin)
                    .Include(u => u.Nurse)
                    .FirstOrDefaultAsync(
                        u => u.Id == userId
                    );

            if (
                user == null ||
                !user.IsActive
            )
            {
                throw new UnauthorizedAccessException();
            }

            if (
                user.Role ==
                    RoleNames.ClinicAdmin &&
                user.Admin?.ClinicId != null
            )
            {
                return user.Admin.ClinicId.Value;
            }

            if (
                user.Role == RoleNames.Nurse &&
                user.Nurse != null
            )
            {
                return user.Nurse.ClinicId;
            }

            throw new UnauthorizedAccessException();
        }

        private IQueryable<MedicationCollection>
            GetCollectionQuery()
        {
            return _context
                .MedicationCollections
                .Include(c => c.Patient)
                    .ThenInclude(p => p.User)
                .Include(c => c.Clinic)
                .Include(c => c.Proxy)
                    .ThenInclude(p => p!.User)
                .Include(c => c.ProcessedByNurse)
                    .ThenInclude(n => n!.User)
                .Include(c => c.Items)
                    .ThenInclude(i => i.Medication)
                .Include(c => c.Items)
                    .ThenInclude(i => i.ClinicStock);
        }

        private async Task<
            MedicationCollectionResponseDto>
            GetDtoAsync(Guid id)
        {
            var collection =
                await GetCollectionQuery()
                    .FirstAsync(
                        c => c.Id == id
                    );

            return ToDto(collection);
        }

        private static MedicationCollectionResponseDto
            ToDto(
                MedicationCollection collection
            )
        {
            var today =
                DateTime.UtcNow.Date;

            var displayStatus =
                collection.Status;

            if (
                collection.Status != "Collected" &&
                collection.Status != "Cancelled"
            )
            {
                displayStatus =
                    collection
                        .ScheduledCollectionDate
                        .Date < today
                        ? "Overdue"
                        : "Pending";
            }

            return new MedicationCollectionResponseDto
            {
                Id =
                    collection.Id,

                PatientId =
                    collection.PatientId,

                PatientName =
                    collection.Patient.User.FullName,

                ClinicId =
                    collection.ClinicId,

                ClinicName =
                    collection.Clinic.Name,

                ProxyId =
                    collection.ProxyId,

                ProxyName =
                    collection.Proxy?.User.FullName,

                ProcessedByNurseId =
                    collection.ProcessedByNurseId,

                ProcessedByNurseName =
                    collection.ProcessedByNurse?
                        .User.FullName,

                ScheduledCollectionDate =
                    collection
                        .ScheduledCollectionDate,

                CollectedAt =
                    collection.CollectedAt,

                Status =
                    displayStatus,

                MedicationName =
                    string.Join(
                        ", ",
                        collection.Items
                            .Select(
                                i =>
                                    i.Medication.Name
                            )
                            .Distinct()
                    ),

                Date =
                    collection
                        .ScheduledCollectionDate
                        .ToString("yyyy-MM-dd"),

                Notes =
                    collection.Notes,

                Items =
                    collection.Items
                        .Select(i =>
                            new MedicationCollectionItemResponseDto
                            {
                                Id =
                                    i.Id,

                                MedicationId =
                                    i.MedicationId,

                                ClinicStockId =
                                    i.ClinicStockId,

                                MedicationName =
                                    i.Medication.Name,

                                Dosage =
                                    i.Medication.Dosage,

                                Form =
                                    i.Medication.Form,

                                Quantity =
                                    i.Quantity,

                                Notes =
                                    i.Notes
                            }
                        )
                        .ToList()
            };
        }
    }
}