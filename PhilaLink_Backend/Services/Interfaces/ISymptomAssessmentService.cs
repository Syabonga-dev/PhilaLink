using PhilaLink_Backend.Models.Entities;

namespace PhilaLink_Backend.Services.Interfaces
{
    public interface ISymptomAssessmentService
    {
        Task<SymptomAssessment> CreateAsync(Guid patientId, string symptoms);
        Task<List<SymptomAssessment>> GetByPatientAsync(Guid patientId);
    }
}