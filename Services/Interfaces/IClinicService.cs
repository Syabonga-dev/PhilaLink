using PhilaLink_Backend.Models.Entities;

namespace PhilaLink_Backend.Services.Interfaces
{
    public interface IClinicService
    {
        Task<Clinic> CreateAsync(string name, string address, string contactNumber);

        Task<List<Clinic>> GetAllAsync();

        Task<Clinic?> GetByIdAsync(Guid id);

        Task<Clinic> UpdateAsync(Guid id, string name, string address, string contactNumber);

        Task<bool> DeleteAsync(Guid id);
    }
}