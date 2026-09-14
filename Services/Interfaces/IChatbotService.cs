using PersonalProject.Models.DTOs;

namespace PersonalProject.Services.Interfaces
{
    public interface IChatbotService
    {
        Task<ChatbotMessageResponseDto> SendMessageAsync(
            Guid userId,
            ChatbotMessageRequestDto dto
        );

        Task<ChatbotHistoryDto?> GetHistoryAsync(
            Guid userId
        );

        Task ClearHistoryAsync(
            Guid userId
        );
    }
}