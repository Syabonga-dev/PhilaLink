using PersonalProject.Models.Entities;

namespace PersonalProject.Services.Interfaces
{
    public interface ISymptomAssessmentService
    {
        Task<SymptomAssessment> CreateAsync(Guid patientId, string symptoms);
        Task<List<SymptomAssessment>> GetByPatientAsync(Guid patientId);
    }
}

