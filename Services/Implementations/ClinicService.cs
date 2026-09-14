using Microsoft.EntityFrameworkCore;
using PersonalProject.Data;
using PersonalProject.Models.DTOs;
using PersonalProject.Models.Entities;
using PersonalProject.Services.Interfaces;

namespace PersonalProject.Services.Implementations
{
    public class ClinicService : IClinicService
    {
        private readonly PhilaLinkDbContext _context;

        public ClinicService(
            PhilaLinkDbContext context
        )
        {
            _context = context;
        }

        public async Task<ClinicResponseDto>
            CreateAsync(
                CreateClinicDto dto
            )
        {
            if (
                string.IsNullOrWhiteSpace(
                    dto.Name
                )
            )
            {
                throw new InvalidOperationException(
                    "Clinic name is required."
                );
            }

            var clinic =
                new Clinic
                {
                    Id =
                        Guid.NewGuid(),

                    Name =
                        dto.Name.Trim(),

                    Type =
                        string.IsNullOrWhiteSpace(
                            dto.Type
                        )
                            ? "Clinic"
                            : dto.Type.Trim(),

                    Address =
                        dto.Address.Trim(),

                    ContactNumber =
                        dto.ContactNumber.Trim(),

                    Latitude =
                        dto.Latitude,

                    Longitude =
                        dto.Longitude,

                    Services =
                        dto.Services.Trim(),

                    OpeningTime =
                        dto.OpeningTime,

                    ClosingTime =
                        dto.ClosingTime,

                    IsActive =
                        true,

                    CreatedAt =
                        DateTime.UtcNow
                };

            _context.Clinics.Add(clinic);

            await _context.SaveChangesAsync();

            return ToDto(clinic);
        }

        public async Task<List<ClinicResponseDto>>
            GetAllAsync()
        {
            return await _context.Clinics
                .Where(c => c.IsActive)
                .OrderBy(c => c.Name)
                .Select(c =>
                    new ClinicResponseDto
                    {
                        Id = c.Id,
                        Name = c.Name,
                        Type = c.Type,
                        Address = c.Address,
                        ContactNumber =
                            c.ContactNumber,
                        Latitude =
                            c.Latitude,
                        Longitude =
                            c.Longitude,
                        Services =
                            c.Services,
                        OpeningTime =
                            c.OpeningTime,
                        ClosingTime =
                            c.ClosingTime,
                        IsActive =
                            c.IsActive
                    }
                )
                .ToListAsync();
        }

        public async Task<ClinicResponseDto?>
            GetByIdAsync(
                Guid id
            )
        {
            var clinic =
                await _context.Clinics
                    .FirstOrDefaultAsync(
                        c =>
                            c.Id == id &&
                            c.IsActive
                    );

            return clinic == null
                ? null
                : ToDto(clinic);
        }

        public async Task<ClinicResponseDto>
            UpdateAsync(
                Guid id,
                UpdateClinicDto dto
            )
        {
            var clinic =
                await _context.Clinics
                    .FirstOrDefaultAsync(
                        c => c.Id == id
                    );

            if (clinic == null)
            {
                throw new KeyNotFoundException(
                    "Clinic not found."
                );
            }

            clinic.Name =
                dto.Name.Trim();

            clinic.Type =
                dto.Type.Trim();

            clinic.Address =
                dto.Address.Trim();

            clinic.ContactNumber =
                dto.ContactNumber.Trim();

            clinic.Latitude =
                dto.Latitude;

            clinic.Longitude =
                dto.Longitude;

            clinic.Services =
                dto.Services.Trim();

            clinic.OpeningTime =
                dto.OpeningTime;

            clinic.ClosingTime =
                dto.ClosingTime;

            clinic.IsActive =
                dto.IsActive;

            clinic.UpdatedAt =
                DateTime.UtcNow;

            await _context.SaveChangesAsync();

            return ToDto(clinic);
        }

        public async Task DeactivateAsync(
            Guid id
        )
        {
            var clinic =
                await _context.Clinics
                    .FirstOrDefaultAsync(
                        c => c.Id == id
                    );

            if (clinic == null)
            {
                throw new KeyNotFoundException(
                    "Clinic not found."
                );
            }

            clinic.IsActive = false;
            clinic.UpdatedAt =
                DateTime.UtcNow;

            await _context.SaveChangesAsync();
        }

        public async Task ActivateAsync(
            Guid id
        )
        {
            var clinic =
                await _context.Clinics
                    .FirstOrDefaultAsync(
                        c => c.Id == id
                    );

            if (clinic == null)
            {
                throw new KeyNotFoundException(
                    "Clinic not found."
                );
            }

            clinic.IsActive = true;
            clinic.UpdatedAt =
                DateTime.UtcNow;

            await _context.SaveChangesAsync();
        }

        private static ClinicResponseDto ToDto(
            Clinic clinic
        )
        {
            return new ClinicResponseDto
            {
                Id =
                    clinic.Id,

                Name =
                    clinic.Name,

                Type =
                    clinic.Type,

                Address =
                    clinic.Address,

                ContactNumber =
                    clinic.ContactNumber,

                Latitude =
                    clinic.Latitude,

                Longitude =
                    clinic.Longitude,

                Services =
                    clinic.Services,

                OpeningTime =
                    clinic.OpeningTime,

                ClosingTime =
                    clinic.ClosingTime,

                IsActive =
                    clinic.IsActive
            };
        }
    }
}