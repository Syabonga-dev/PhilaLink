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
        private const string EmergencyResponse =
            "Your message contains symptoms or information that may indicate a medical emergency. " +
            "Please seek emergency medical help now or go to the nearest emergency facility. " +
            "Do not rely on PhilaLink or the chatbot for emergency treatment.";

        private const string UrgentResponse =
            "Your message contains symptoms that should be assessed promptly by a healthcare professional. " +
            "Please contact your clinic, an urgent care service, or another qualified healthcare provider as soon as possible. " +
            "If you are struggling to breathe, develop chest pain, faint, become confused, have severe bleeding, or your symptoms rapidly worsen, seek emergency medical help immediately.";

        private readonly PhilaLinkDbContext _context;
        private readonly IChatbotProvider _provider;

        public ChatbotService(
            PhilaLinkDbContext context,
            IChatbotProvider provider
        )
        {
            _context = context;
            _provider = provider;
        }

        public async Task<ChatbotMessageResponseDto>
            SendMessageAsync(
                Guid userId,
                ChatbotMessageRequestDto dto
            )
        {
            if (
                string.IsNullOrWhiteSpace(
                    dto.Message
                )
            )
            {
                throw new InvalidOperationException(
                    "Message is required."
                );
            }

            var patient =
                await GetActivePatientAsync(
                    userId
                );

            var cleanMessage =
                dto.Message.Trim();

            var conversation =
                await _context.ChatConversations
                    .Include(c => c.Messages)
                    .FirstOrDefaultAsync(
                        c =>
                            c.PatientId ==
                                patient.Id &&
                            c.IsActive
                    );

            if (conversation == null)
            {
                conversation =
                    new ChatConversation
                    {
                        Id =
                            Guid.NewGuid(),

                        PatientId =
                            patient.Id,

                        StartedAt =
                            DateTime.UtcNow,

                        UpdatedAt =
                            DateTime.UtcNow,

                        IsActive =
                            true
                    };

                _context.ChatConversations.Add(
                    conversation
                );
            }

            var userMessage =
                new ChatMessage
                {
                    Id =
                        Guid.NewGuid(),

                    ConversationId =
                        conversation.Id,

                    Role =
                        "user",

                    Content =
                        cleanMessage,

                    CreatedAt =
                        DateTime.UtcNow
                };

            _context.ChatMessages.Add(
                userMessage
            );

            conversation.Messages.Add(
                userMessage
            );

            conversation.UpdatedAt =
                DateTime.UtcNow;

            if (
                ContainsAny(
                    cleanMessage,
                    EmergencyKeywords
                )
            )
            {
                return await SaveInterceptedResponseAsync(
                    conversation,
                    EmergencyResponse
                );
            }

            if (
                ContainsAny(
                    cleanMessage,
                    UrgentKeywords
                )
            )
            {
                return await SaveInterceptedResponseAsync(
                    conversation,
                    UrgentResponse
                );
            }

            await _context.SaveChangesAsync();

            var patientContext =
                await BuildPatientContextAsync(
                    patient.Id
                );

            var messages =
                conversation.Messages
                    .OrderBy(
                        m => m.CreatedAt
                    )
                    .ToList();

            var responseText =
                await _provider.GenerateResponseAsync(
                    patientContext,
                    messages
                );

            if (
                string.IsNullOrWhiteSpace(
                    responseText
                )
            )
            {
                responseText =
                    "I could not generate a response right now.";
            }

            var assistantMessage =
                CreateAssistantMessage(
                    conversation.Id,
                    responseText.Trim()
                );

            _context.ChatMessages.Add(
                assistantMessage
            );

            conversation.Messages.Add(
                assistantMessage
            );

            conversation.UpdatedAt =
                assistantMessage.CreatedAt;

            await _context.SaveChangesAsync();

            return CreateResponse(
                conversation,
                assistantMessage
            );
        }

        public async Task<ChatbotHistoryDto?>
            GetHistoryAsync(
                Guid userId
            )
        {
            var patient =
                await GetActivePatientAsync(
                    userId
                );

            var conversation =
                await _context.ChatConversations
                    .Include(c => c.Messages)
                    .FirstOrDefaultAsync(
                        c =>
                            c.PatientId ==
                                patient.Id &&
                            c.IsActive
                    );

            if (conversation == null)
            {
                return null;
            }

            return new ChatbotHistoryDto
            {
                ConversationId =
                    conversation.Id,

                StartedAt =
                    conversation.StartedAt,

                UpdatedAt =
                    conversation.UpdatedAt,

                Messages =
                    conversation.Messages
                        .OrderBy(
                            m => m.CreatedAt
                        )
                        .Select(
                            m =>
                                new ChatbotHistoryMessageDto
                                {
                                    Id =
                                        m.Id,

                                    Role =
                                        m.Role,

                                    Content =
                                        m.Content,

                                    CreatedAt =
                                        m.CreatedAt
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
                await GetActivePatientAsync(
                    userId
                );

            var conversations =
                await _context.ChatConversations
                    .Where(
                        c =>
                            c.PatientId ==
                                patient.Id &&
                            c.IsActive
                    )
                    .ToListAsync();

            foreach (
                var conversation
                in conversations
            )
            {
                conversation.IsActive =
                    false;

                conversation.UpdatedAt =
                    DateTime.UtcNow;
            }

            await _context.SaveChangesAsync();
        }

        private async Task<ChatbotMessageResponseDto>
            SaveInterceptedResponseAsync(
                ChatConversation conversation,
                string responseText
            )
        {
            var assistantMessage =
                CreateAssistantMessage(
                    conversation.Id,
                    responseText
                );

            _context.ChatMessages.Add(
                assistantMessage
            );

            conversation.Messages.Add(
                assistantMessage
            );

            conversation.UpdatedAt =
                assistantMessage.CreatedAt;

            await _context.SaveChangesAsync();

            return CreateResponse(
                conversation,
                assistantMessage
            );
        }

        private async Task<Patient>
            GetActivePatientAsync(
                Guid userId
            )
        {
            var patient =
                await _context.Patients
                    .Include(p => p.User)
                    .FirstOrDefaultAsync(
                        p =>
                            p.UserId ==
                                userId &&
                            p.User.Role ==
                                RoleNames.Patient &&
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

        private async Task<string>
            BuildPatientContextAsync(
                Guid patientId
            )
        {
            var medications =
                await _context.Medications
                    .Where(
                        m =>
                            m.PatientId ==
                                patientId &&
                            m.IsActive
                    )
                    .Select(
                        m =>
                            new
                            {
                                m.Name,
                                m.Dosage,
                                m.Form,
                                m.Instructions
                            }
                    )
                    .ToListAsync();

            var allergies =
                await _context.Allergies
                    .Where(
                        a =>
                            a.PatientId ==
                            patientId
                    )
                    .Select(
                        a =>
                            a.AllergyName
                    )
                    .ToListAsync();

            var conditions =
                await _context.MedicalConditions
                    .Where(
                        c =>
                            c.PatientId ==
                                patientId
                    )
                    .Select(
                        c =>
                            c.ConditionName
                    )
                    .ToListAsync();

            var medicationSummary =
                medications.Count == 0
                    ? "None recorded"
                    : string.Join(
                        "; ",
                        medications.Select(
                            m =>
                                $"{m.Name} {m.Dosage} {m.Form}".Trim()
                        )
                    );

            return
                $"Active medications: {medicationSummary}\n" +
                $"Allergies: {(allergies.Count == 0 ? "None recorded" : string.Join(", ", allergies))}\n" +
                $"Medical conditions: {(conditions.Count == 0 ? "None recorded" : string.Join(", ", conditions))}";
        }

        private static bool ContainsAny(
            string text,
            IEnumerable<string> keywords
        )
        {
            return keywords.Any(
                keyword =>
                    text.Contains(
                        keyword,
                        StringComparison.OrdinalIgnoreCase
                    )
            );
        }

        private static readonly string[]
            EmergencyKeywords =
        {
            "chest pain",
            "cannot breathe",
            "can't breathe",
            "unable to breathe",
            "not breathing",
            "difficulty breathing",
            "severe shortness of breath",
            "gasping",
            "blue lips",
            "blue face",
            "unconscious",
            "unresponsive",
            "severe bleeding",
            "bleeding heavily",
            "seizure",
            "convulsion",
            "overdose",
            "anaphylaxis",
            "severe allergic reaction",
            "face drooping",
            "slurred speech",
            "sudden weakness",
            "suicidal",
            "suicide"
        };

        private static readonly string[]
            UrgentKeywords =
        {
            "shortness of breath",
            "breathless",
            "breathlessness",
            "wheezing",
            "worsening asthma",
            "high fever",
            "persistent fever",
            "persistent vomiting",
            "vomiting repeatedly",
            "blood in stool",
            "blood in urine",
            "coughing blood",
            "fainting",
            "passed out",
            "severe pain",
            "dehydration",
            "dehydrated"
        };

        private static ChatMessage
            CreateAssistantMessage(
                Guid conversationId,
                string content
            )
        {
            return new ChatMessage
            {
                Id =
                    Guid.NewGuid(),

                ConversationId =
                    conversationId,

                Role =
                    "assistant",

                Content =
                    content,

                CreatedAt =
                    DateTime.UtcNow
            };
        }

        private static ChatbotMessageResponseDto
            CreateResponse(
                ChatConversation conversation,
                ChatMessage assistantMessage
            )
        {
            return new ChatbotMessageResponseDto
            {
                ConversationId =
                    conversation.Id,

                Message =
                    assistantMessage.Content,

                CreatedAt =
                    assistantMessage.CreatedAt
            };
        }
    }
}
