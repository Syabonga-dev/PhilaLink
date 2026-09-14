using PersonalProject.Models.Entities;

namespace PersonalProject.Services.Interfaces
{
    public interface ISymptomAssessmentService
    {
        Task<SymptomAssessment> CreateForPatientAsync(Guid userId, string symptoms);

        Task<List<SymptomAssessment>> GetMyAssessmentsAsync(Guid userId);
    }
}