using PersonalProject.Models.Entities;

namespace PersonalProject.Services.Interfaces
{
    public interface IPatientService
    {
        Task<Patient?> GetPatientByIdAsync(Guid id);
        Task<Patient?> GetPatientByUserIdAsync(Guid userId);
        Task<List<Patient>> GetAllPatientsAsync();
        Task<string> CreatePatientAsync(Patient patient);
        Task<string> UpdatePatientAsync(Guid id, Patient patient);
        Task<string> DeletePatientAsync(Guid id);
    }
}