using PersonalProject.Models.Entities;

namespace PersonalProject.Services.AI
{
    public class SafeFallbackChatbotProvider :
        IChatbotProvider
    {
        public Task<string> GenerateResponseAsync(
            string patientContext,
            IReadOnlyList<ChatMessage> messages
        )
        {
            return Task.FromResult(
                "The PhilaLink health assistant is temporarily unavailable. " +
                "Please use the symptom assessment feature or contact your clinic " +
                "if you need medical assistance."
            );
        }
    }
}