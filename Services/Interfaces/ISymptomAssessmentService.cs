using PersonalProject.Models.DTOs;
using PersonalProject.Models.Entities;

namespace PersonalProject.Services.Interfaces
{
    public interface ISymptomAssessmentService
    {
        Task<SymptomAssessment> CreateForPatientAsync(
            Guid userId,
            SymptomCreateDto dto
        );

        Task<List<SymptomAssessment>> GetMyAssessmentsAsync(
            Guid userId
        );
    }
}
