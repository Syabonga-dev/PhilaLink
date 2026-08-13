using PersonalProject.Data;
using PersonalProject.Models.Entities;
using PersonalProject.Services.Interfaces;
using Microsoft.EntityFrameworkCore;

public class ClinicService : IClinicService
{
    private readonly PhilaLinkDbContext _context;

    public ClinicService(PhilaLinkDbContext context)
    {
        _context = context;
    }

    public async Task<Clinic> CreateAsync(string name, string address, string contactNumber)
    {
        var clinic = new Clinic
        {
            Id = Guid.NewGuid(),
            Name = name,
            Address = address,
            ContactNumber = contactNumber
        };

        _context.Clinics.Add(clinic);
        await _context.SaveChangesAsync();

        return clinic;
    }

    public async Task<List<Clinic>> GetAllAsync()
    {
        return await _context.Clinics.ToListAsync();
    }

    public async Task<Clinic?> GetByIdAsync(Guid id)
    {
        return await _context.Clinics.FindAsync(id);
    }

    public async Task<Clinic> UpdateAsync(Guid id, string name, string address, string contactNumber)
    {
        var clinic = await _context.Clinics.FindAsync(id);
        if (clinic == null) throw new Exception("Clinic not found");

        clinic.Name = name;
        clinic.Address = address;
        clinic.ContactNumber = contactNumber;

        await _context.SaveChangesAsync();
        return clinic;
    }

    public async Task<bool> DeleteAsync(Guid id)
    {
        var clinic = await _context.Clinics.FindAsync(id);
        if (clinic == null) return false;

        _context.Clinics.Remove(clinic);
        await _context.SaveChangesAsync();
        return true;
    }
}

