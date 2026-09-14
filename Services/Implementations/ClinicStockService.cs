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
        private readonly PhilaLinkDbContext _context;
        private readonly IAuditLogService _audit;

        public ClinicStockService(
            PhilaLinkDbContext context,
            IAuditLogService audit
        )
        {
            _context = context;
            _audit = audit;
        }

        public async Task<List<ClinicStockResponseDto>>
            GetAllAsync(Guid performedByUserId)
        {
            var clinicId =
                await GetStaffClinicIdAsync(
                    performedByUserId
                );

            var stock =
                await _context.ClinicStocks
                    .Include(s => s.Clinic)
                    .Where(
                        s => s.ClinicId == clinicId
                    )
                    .OrderBy(s => s.MedicationName)
                    .ThenBy(s => s.Strength)
                    .ToListAsync();

            return stock
                .Select(ToDto)
                .ToList();
        }

        public async Task<ClinicStockResponseDto>
            CreateAsync(
                CreateClinicStockDto dto,
                Guid performedByUserId
            )
        {
            var clinicId = await GetStaffClinicIdAsync(performedByUserId);

            if (dto.ClinicId != clinicId)
            {
                throw new UnauthorizedAccessException(
                    "You can only manage stock for your clinic."
                );
            }

            ValidateQuantities(
                dto.QuantityOnHand,
                dto.ReorderLevel
            );

            var duplicate =
                await _context.ClinicStocks
                    .AnyAsync(s =>
                        s.ClinicId == clinicId &&
                        s.MedicationName ==
                            dto.MedicationName.Trim() &&
                        s.Strength ==
                            dto.Strength.Trim() &&
                        s.Form ==
                            dto.Form.Trim()
                    );

            if (duplicate)
            {
                throw new InvalidOperationException(
                    "This stock item already exists for the clinic."
                );
            }

            var stock = new ClinicStock
            {
                Id = Guid.NewGuid(),

                ClinicId = clinicId,

                MedicationName =
                    dto.MedicationName.Trim(),

                Strength =
                    dto.Strength.Trim(),

                Form =
                    dto.Form.Trim(),

                Unit =
                    dto.Unit.Trim(),

                QuantityOnHand =
                    dto.QuantityOnHand,

                ReorderLevel =
                    dto.ReorderLevel,

                IsActive = true,

                CreatedAt = DateTime.UtcNow
            };

            _context.ClinicStocks.Add(stock);

            await _context.SaveChangesAsync();

            await _audit.LogAsync(
                "ClinicStockCreated",
                performedByUserId,
                $"Stock item {stock.Id} created."
            );

            return await GetDtoAsync(stock.Id);
        }

        public async Task<ClinicStockResponseDto>
            UpdateAsync(
                Guid stockId,
                UpdateClinicStockDto dto,
                Guid performedByUserId
            )
        {
            var clinicId =
                await GetStaffClinicIdAsync(
                    performedByUserId
                );

            ValidateQuantities(
                dto.QuantityOnHand,
                dto.ReorderLevel
            );

            var stock =
                await _context.ClinicStocks
                    .FirstOrDefaultAsync(
                        s =>
                            s.Id == stockId &&
                            s.ClinicId == clinicId
                    );

            if (stock == null)
            {
                throw new KeyNotFoundException(
                    "Clinic stock item not found."
                );
            }

            stock.QuantityOnHand = dto.QuantityOnHand;
            stock.ReorderLevel = dto.ReorderLevel;
            stock.IsActive = dto.IsActive;
            stock.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            await _audit.LogAsync(
                "ClinicStockUpdated",
                performedByUserId,
                $"Stock item {stock.Id} updated."
            );

            return await GetDtoAsync(stock.Id);
        }

        public async Task<ClinicStockResponseDto>
            AdjustAsync(
                Guid stockId,
                AdjustClinicStockDto dto,
                Guid performedByUserId
            )
        {
            if (dto.QuantityChange == 0)
            {
                throw new InvalidOperationException(
                    "Quantity change cannot be zero."
                );
            }

            var clinicId =
                await GetStaffClinicIdAsync(
                    performedByUserId
                );

            var stock =
                await _context.ClinicStocks
                    .FirstOrDefaultAsync(
                        s =>
                            s.Id == stockId &&
                            s.ClinicId == clinicId
                    );

            if (stock == null)
            {
                throw new KeyNotFoundException(
                    "Clinic stock item not found."
                );
            }

            var newQuantity =
                stock.QuantityOnHand +
                dto.QuantityChange;

            if (newQuantity < 0)
            {
                throw new InvalidOperationException(
                    "Stock quantity cannot become negative."
                );
            }

            stock.QuantityOnHand = newQuantity;
            stock.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            await _audit.LogAsync(
                "ClinicStockAdjusted",
                performedByUserId,
                $"Stock item {stock.Id} changed by " +
                $"{dto.QuantityChange}. " +
                $"Reason: {dto.Reason ?? "Not supplied"}"
            );

            return await GetDtoAsync(stock.Id);
        }

        private async Task<ClinicStockResponseDto>
            GetDtoAsync(Guid id)
        {
            var stock =
                await _context.ClinicStocks
                    .Include(s => s.Clinic)
                    .FirstAsync(s => s.Id == id);

            return ToDto(stock);
        }

        private async Task<Guid>
            GetStaffClinicIdAsync(
                Guid userId
            )
        {
            var user = await _context.Users
                .Include(u => u.Admin)
                .Include(u => u.Nurse)
                .FirstOrDefaultAsync(
                    u => u.Id == userId
                );

            if (user == null || !user.IsActive)
            {
                throw new UnauthorizedAccessException();
            }

            if (
                user.Role == RoleNames.ClinicAdmin &&
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

        private static void ValidateQuantities(
            int quantity,
            int reorderLevel
        )
        {
            if (quantity < 0)
            {
                throw new InvalidOperationException(
                    "Quantity on hand cannot be negative."
                );
            }

            if (reorderLevel < 0)
            {
                throw new InvalidOperationException(
                    "Reorder level cannot be negative."
                );
            }
        }

        private static ClinicStockResponseDto ToDto(
            ClinicStock stock
        )
        {
            return new ClinicStockResponseDto
            {
                Id = stock.Id,

                ClinicId = stock.ClinicId,

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