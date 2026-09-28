using Microsoft.EntityFrameworkCore;
using PersonalProject.Data;
using PersonalProject.Models.Constants;
using PersonalProject.Models.DTOs;
using PersonalProject.Models.Entities;
using PersonalProject.Services.Interfaces;

namespace PersonalProject.Services.Implementations
{
    public class ClinicStockService :
        IClinicStockService
    {
        private readonly PhilaLinkDbContext
            _context;

        private readonly IAuditLogService
            _audit;

        public ClinicStockService(
            PhilaLinkDbContext context,
            IAuditLogService audit
        )
        {
            _context =
                context;

            _audit =
                audit;
        }

        // =====================================================
        // LIST STOCK
        // =====================================================

        public async Task<List<ClinicStockResponseDto>>
            GetAllAsync(
                Guid performedByUserId
            )
        {
            var clinicId =
                await GetClinicAdminClinicIdAsync(
                    performedByUserId
                );

            var stock =
                await _context.ClinicStocks
                    .AsNoTracking()
                    .Include(
                        item =>
                            item.Clinic
                    )
                    .Where(
                        item =>
                            item.ClinicId ==
                                clinicId
                    )
                    .OrderBy(
                        item =>
                            item.MedicationName
                    )
                    .ThenBy(
                        item =>
                            item.Strength
                    )
                    .ToListAsync();

            return stock
                .Select(
                    ToDto
                )
                .ToList();
        }

        // =====================================================
        // CREATE
        // =====================================================

        public async Task<ClinicStockResponseDto>
            CreateAsync(
                CreateClinicStockDto dto,
                Guid performedByUserId
            )
        {
            var clinicId =
                await GetClinicAdminClinicIdAsync(
                    performedByUserId
                );

            if (
                dto.ClinicId !=
                clinicId
            )
            {
                throw new UnauthorizedAccessException(
                    "You can only manage stock for your own clinic."
                );
            }

            ValidateQuantities(
                dto.QuantityOnHand,
                dto.ReorderLevel
            );

            if (
                string.IsNullOrWhiteSpace(
                    dto.MedicationName
                )
            )
            {
                throw new InvalidOperationException(
                    "Medication name is required."
                );
            }

            if (
                string.IsNullOrWhiteSpace(
                    dto.Strength
                )
            )
            {
                throw new InvalidOperationException(
                    "Strength is required."
                );
            }

            if (
                string.IsNullOrWhiteSpace(
                    dto.Form
                )
            )
            {
                throw new InvalidOperationException(
                    "Medication form is required."
                );
            }

            if (
                string.IsNullOrWhiteSpace(
                    dto.Unit
                )
            )
            {
                throw new InvalidOperationException(
                    "Stock unit is required."
                );
            }

            var medicationName =
                dto.MedicationName
                    .Trim();

            var strength =
                dto.Strength
                    .Trim();

            var form =
                dto.Form
                    .Trim();

            var unit =
                dto.Unit
                    .Trim();

            var duplicate =
                await _context
                    .ClinicStocks
                    .AnyAsync(
                        item =>
                            item.ClinicId ==
                                clinicId &&
                            item.MedicationName ==
                                medicationName &&
                            item.Strength ==
                                strength &&
                            item.Form ==
                                form
                    );

            if (
                duplicate
            )
            {
                throw new InvalidOperationException(
                    "This stock item already exists for the clinic."
                );
            }

            var stock =
                new ClinicStock
                {
                    Id =
                        Guid.NewGuid(),

                    ClinicId =
                        clinicId,

                    MedicationName =
                        medicationName,

                    Strength =
                        strength,

                    Form =
                        form,

                    Unit =
                        unit,

                    QuantityOnHand =
                        dto.QuantityOnHand,

                    ReorderLevel =
                        dto.ReorderLevel,

                    IsActive =
                        true,

                    CreatedAt =
                        DateTime.UtcNow
                };

            _context.ClinicStocks
                .Add(
                    stock
                );

            await _context
                .SaveChangesAsync();

            await _audit
                .LogAsync(
                    "ClinicStockCreated",
                    performedByUserId,
                    $"Stock item {stock.Id} created."
                );

            return await GetDtoAsync(
                stock.Id,
                clinicId
            );
        }

        // =====================================================
        // UPDATE
        // =====================================================

        public async Task<ClinicStockResponseDto>
            UpdateAsync(
                Guid stockId,
                UpdateClinicStockDto dto,
                Guid performedByUserId
            )
        {
            var clinicId =
                await GetClinicAdminClinicIdAsync(
                    performedByUserId
                );

            ValidateQuantities(
                dto.QuantityOnHand,
                dto.ReorderLevel
            );

            var stock =
                await _context
                    .ClinicStocks
                    .FirstOrDefaultAsync(
                        item =>
                            item.Id ==
                                stockId &&
                            item.ClinicId ==
                                clinicId
                    );

            if (
                stock ==
                null
            )
            {
                throw new KeyNotFoundException(
                    "Clinic stock item not found."
                );
            }

            stock.QuantityOnHand =
                dto.QuantityOnHand;

            stock.ReorderLevel =
                dto.ReorderLevel;

            stock.IsActive =
                dto.IsActive;

            stock.UpdatedAt =
                DateTime.UtcNow;

            await _context
                .SaveChangesAsync();

            await _audit
                .LogAsync(
                    "ClinicStockUpdated",
                    performedByUserId,
                    $"Stock item {stock.Id} updated."
                );

            return await GetDtoAsync(
                stock.Id,
                clinicId
            );
        }

        // =====================================================
        // ADJUST QUANTITY
        // =====================================================

        public async Task<ClinicStockResponseDto>
            AdjustAsync(
                Guid stockId,
                AdjustClinicStockDto dto,
                Guid performedByUserId
            )
        {
            if (
                dto.QuantityChange ==
                0
            )
            {
                throw new InvalidOperationException(
                    "Quantity change cannot be zero."
                );
            }

            var clinicId =
                await GetClinicAdminClinicIdAsync(
                    performedByUserId
                );

            var stock =
                await _context
                    .ClinicStocks
                    .FirstOrDefaultAsync(
                        item =>
                            item.Id ==
                                stockId &&
                            item.ClinicId ==
                                clinicId
                    );

            if (
                stock ==
                null
            )
            {
                throw new KeyNotFoundException(
                    "Clinic stock item not found."
                );
            }

            var newQuantity =
                stock.QuantityOnHand +
                dto.QuantityChange;

            if (
                newQuantity <
                0
            )
            {
                throw new InvalidOperationException(
                    "Stock quantity cannot become negative."
                );
            }

            stock.QuantityOnHand =
                newQuantity;

            stock.UpdatedAt =
                DateTime.UtcNow;

            await _context
                .SaveChangesAsync();

            await _audit
                .LogAsync(
                    "ClinicStockAdjusted",
                    performedByUserId,
                    $"Stock item {stock.Id} changed by " +
                    $"{dto.QuantityChange}. " +
                    $"Reason: {dto.Reason ?? "Not supplied"}"
                );

            return await GetDtoAsync(
                stock.Id,
                clinicId
            );
        }

        // =====================================================
        // GET ONE
        // =====================================================

        private async Task<ClinicStockResponseDto>
            GetDtoAsync(
                Guid stockId,
                Guid clinicId
            )
        {
            var stock =
                await _context
                    .ClinicStocks
                    .AsNoTracking()
                    .Include(
                        item =>
                            item.Clinic
                    )
                    .FirstOrDefaultAsync(
                        item =>
                            item.Id ==
                                stockId &&
                            item.ClinicId ==
                                clinicId
                    );

            if (
                stock ==
                null
            )
            {
                throw new KeyNotFoundException(
                    "Clinic stock item not found."
                );
            }

            return ToDto(
                stock
            );
        }

        // =====================================================
        // CLINIC ADMIN BOUNDARY
        // =====================================================

        private async Task<Guid>
            GetClinicAdminClinicIdAsync(
                Guid userId
            )
        {
            var user =
                await _context.Users
                    .AsNoTracking()
                    .Include(
                        item =>
                            item.Admin
                    )
                    .FirstOrDefaultAsync(
                        item =>
                            item.Id ==
                                userId
                    );

            if (
                user ==
                    null ||
                !user.IsActive ||
                user.Role !=
                    RoleNames.ClinicAdmin ||
                user.Admin?.ClinicId ==
                    null
            )
            {
                throw new UnauthorizedAccessException(
                    "An active ClinicAdmin account with an assigned clinic is required."
                );
            }

            return user.Admin
                .ClinicId
                .Value;
        }

        // =====================================================
        // VALIDATION
        // =====================================================

        private static void
            ValidateQuantities(
                int quantity,
                int reorderLevel
            )
        {
            if (
                quantity <
                0
            )
            {
                throw new InvalidOperationException(
                    "Quantity on hand cannot be negative."
                );
            }

            if (
                reorderLevel <
                0
            )
            {
                throw new InvalidOperationException(
                    "Reorder level cannot be negative."
                );
            }
        }

        // =====================================================
        // DTO
        // =====================================================

        private static ClinicStockResponseDto
            ToDto(
                ClinicStock stock
            )
        {
            return new ClinicStockResponseDto
            {
                Id =
                    stock.Id,

                ClinicId =
                    stock.ClinicId,

                ClinicName =
                    stock.Clinic.Name,

                MedicationName =
                    stock.MedicationName,

                Strength =
                    stock.Strength,

                Form =
                    stock.Form,

                Unit =
                    stock.Unit,

                QuantityOnHand =
                    stock.QuantityOnHand,

                ReorderLevel =
                    stock.ReorderLevel,

                IsActive =
                    stock.IsActive
            };
        }
    }
}