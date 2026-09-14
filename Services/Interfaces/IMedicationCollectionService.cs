using PersonalProject.Models.DTOs;

namespace PersonalProject.Services.Interfaces
{
    public interface IMedicationCollectionService
    {
        Task<MedicationCollectionResponseDto> CreateAsync(CreateMedicationCollectionDto dto, Guid performedByUserId);

        Task<List<MedicationCollectionResponseDto>> GetClinicCollectionsAsync(Guid performedByUserId);

        Task<MedicationCollectionSummaryDto> GetSummaryAsync(Guid performedByUserId);

        Task<MedicationCollectionResponseDto> AssignProxyAsync(Guid collectionId, Guid proxyId, Guid performedByUserId);

        Task<MedicationCollectionResponseDto> CompleteAsync(Guid collectionId, CompleteMedicationCollectionDto dto, Guid performedByUserId);
    }
}