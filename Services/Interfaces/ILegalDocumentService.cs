using PersonalProject.Models.DTOs.Legal;

namespace PersonalProject.Services.Interfaces
{
    public interface ILegalDocumentService
    {
        Task<LegalStatusDto> GetLegalStatusAsync(Guid userId);

        Task<LegalDocumentDto> AcceptLegalDocumentAsync(
            Guid userId,
            AcceptLegalDocumentRequest request
        );
    }
}