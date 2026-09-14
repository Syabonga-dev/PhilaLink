using PersonalProject.Models.Entities;

namespace PersonalProject.Services.AI
{
    public interface IChatbotProvider
    {
        Task<string> GenerateResponseAsync(
            string patientContext,
            IReadOnlyList<ChatMessage> messages
        );
    }
}