using Microsoft.EntityFrameworkCore;
using PersonalProject.Data;
using PersonalProject.Models.Constants;
using PersonalProject.Models.DTOs;
using PersonalProject.Models.Entities;
using PersonalProject.Services.AI;
using PersonalProject.Services.Interfaces;

namespace PersonalProject.Services.Implementations
{
    public class ChatbotService : IChatbotService
    {
        private readonly PhilaLinkDbContext _context;
        private readonly IChatbotProvider _provider;

        public ChatbotService(PhilaLinkDbContext context, IChatbotProvider provider)
        {
            _context = context;
            _provider = provider;
        }

        public async Task<ChatbotMessageResponseDto> SendMessageAsync(Guid userId, ChatbotMessageRequestDto dto)
        {
            if (string.IsNullOrWhiteSpace(dto.Message))
            {
                throw new InvalidOperationException("Message is required.");
            }

            var patient = await GetActivePatientAsync(userId);

            var conversation = await _context.ChatConversations.Include(c => c.Messages).FirstOrDefaultAsync(c => c.PatientId == patient.Id && c.IsActive);

            if (conversation == null)
            {
                conversation = new ChatConversation
                {
                    Id = Guid.NewGuid(),
                    PatientId = patient.Id,
                    StartedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow,
                    IsActive = true
                };

                _context.ChatConversations.Add(conversation);
            }

            var userMessage = new ChatMessage
            {
                Id = Guid.NewGuid(),
                ConversationId = conversation.Id,
                Role = "user",
                Content = dto.Message.Trim(),
                CreatedAt = DateTime.UtcNow
            };

            _context.ChatMessages.Add(userMessage);
            conversation.Messages.Add(userMessage);

            conversation.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            var patientContext = await BuildPatientContextAsync(patient.Id);

            var messages = conversation.Messages
                    .OrderBy(m => m.CreatedAt)
                    .ToList();

            var responseText =
                await _provider.GenerateResponseAsync(
                    patientContext,
                    messages
                );

            if (string.IsNullOrWhiteSpace(responseText))
            {
                responseText = "I could not generate a response right now.";
            }

            var assistantMessage = new ChatMessage
            {
                Id = Guid.NewGuid(),
                ConversationId = conversation.Id,
                Role = "assistant",
                Content = responseText.Trim(),
                CreatedAt = DateTime.UtcNow
            };

            _context.ChatMessages.Add(assistantMessage);

            conversation.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            return new ChatbotMessageResponseDto
            {
                ConversationId = conversation.Id,
                Message = assistantMessage.Content,
                CreatedAt = assistantMessage.CreatedAt
            };
        }

        public async Task<ChatbotHistoryDto?> GetHistoryAsync(
            Guid userId
        )
        {
            var patient =
                await GetActivePatientAsync(userId);

            var conversation =
                await _context.ChatConversations
                    .Include(c => c.Messages)
                    .FirstOrDefaultAsync(
                        c =>
                            c.PatientId == patient.Id &&
                            c.IsActive
                    );

            if (conversation == null)
            {
                return null;
            }

            return new ChatbotHistoryDto
            {
                ConversationId = conversation.Id,
                StartedAt = conversation.StartedAt,
                UpdatedAt = conversation.UpdatedAt,

                Messages =
                    conversation.Messages
                        .OrderBy(m => m.CreatedAt)
                        .Select(
                            m =>
                                new ChatbotHistoryMessageDto
                                {
                                    Id = m.Id,
                                    Role = m.Role,
                                    Content = m.Content,
                                    CreatedAt = m.CreatedAt
                                }
                        )
                        .ToList()
            };
        }

        public async Task ClearHistoryAsync(
            Guid userId
        )
        {
            var patient =
                await GetActivePatientAsync(userId);

            var conversations =
                await _context.ChatConversations
                    .Where(
                        c =>
                            c.PatientId == patient.Id &&
                            c.IsActive
                    )
                    .ToListAsync();

            foreach (var conversation in conversations)
            {
                conversation.IsActive = false;
                conversation.UpdatedAt = DateTime.UtcNow;
            }

            await _context.SaveChangesAsync();
        }

        private async Task<Patient> GetActivePatientAsync(
            Guid userId
        )
        {
            var patient =
                await _context.Patients
                    .Include(p => p.User)
                    .FirstOrDefaultAsync(
                        p =>
                            p.UserId == userId &&
                            p.User.Role == RoleNames.Patient &&
                            p.User.IsActive
                    );

            if (patient == null)
            {
                throw new UnauthorizedAccessException(
                    "Active Patient account required."
                );
            }

            return patient;
        }

        private async Task<string> BuildPatientContextAsync(
            Guid patientId
        )
        {
            var medications =
                await _context.Medications
                    .Where(
                        m =>
                            m.PatientId == patientId &&
                            m.IsActive
                    )
                    .Select(m => m.Name)
                    .ToListAsync();

            var allergies =
                await _context.Allergies
                    .Where(
                        a =>
                            a.PatientId == patientId
                    )
                    .Select(a => a.AllergyName)
                    .ToListAsync();

            var conditions =
                await _context.MedicalConditions
                    .Where(
                        c =>
                            c.PatientId == patientId
                    )
                    .Select(c => c.ConditionName)
                    .ToListAsync();

            return
                $"Active medications: {string.Join(", ", medications)}\n" +
                $"Allergies: {string.Join(", ", allergies)}\n" +
                $"Medical conditions: {string.Join(", ", conditions)}";
        }
    }
}