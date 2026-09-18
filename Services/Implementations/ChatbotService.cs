using Microsoft.EntityFrameworkCore;
using PersonalProject.Data;
using PersonalProject.Models.Constants;
using PersonalProject.Models.DTOs;
using PersonalProject.Models.Entities;
using PersonalProject.Services.AI;
using PersonalProject.Services.Interfaces;
using System.Text;

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
            "If you are struggling to breathe, develop chest pain, faint, become confused, have severe bleeding, " +
            "or your symptoms rapidly worsen, seek emergency medical help immediately.";

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

            var patientId =
                await GetActivePatientIdAsync(
                    userId
                );

            var cleanMessage =
                dto.Message.Trim();

            /*
             * Tracking is required here because the active
             * conversation is updated and new messages are added.
             */
            var conversation =
                await _context.ChatConversations
                    .Include(
                        conversation =>
                            conversation.Messages
                    )
                    .FirstOrDefaultAsync(
                        conversation =>
                            conversation.PatientId ==
                                patientId &&
                            conversation.IsActive
                    );

            if (conversation == null)
            {
                conversation =
                    new ChatConversation
                    {
                        Id =
                            Guid.NewGuid(),

                        PatientId =
                            patientId,

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

            /*
             * Preserve the current behavior: persist the user's
             * message before calling the external AI provider.
             */
            await _context.SaveChangesAsync();

            var patientContext =
                await BuildPatientContextAsync(
                    patientId
                );

            var messages =
                conversation.Messages
                    .OrderBy(
                        message =>
                            message.CreatedAt
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
            var patientId =
                await GetActivePatientIdAsync(
                    userId
                );

            var conversation =
                await _context.ChatConversations
                    .AsNoTracking()
                    .Include(
                        conversation =>
                            conversation.Messages
                    )
                    .FirstOrDefaultAsync(
                        conversation =>
                            conversation.PatientId ==
                                patientId &&
                            conversation.IsActive
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
                            message =>
                                message.CreatedAt
                        )
                        .Select(
                            message =>
                                new ChatbotHistoryMessageDto
                                {
                                    Id =
                                        message.Id,

                                    Role =
                                        message.Role,

                                    Content =
                                        message.Content,

                                    CreatedAt =
                                        message.CreatedAt
                                }
                        )
                        .ToList()
            };
        }

        public async Task ClearHistoryAsync(
            Guid userId
        )
        {
            var patientId =
                await GetActivePatientIdAsync(
                    userId
                );

            /*
             * Tracking is intentionally kept because these rows
             * are updated before SaveChangesAsync.
             */
            var conversations =
                await _context.ChatConversations
                    .Where(
                        conversation =>
                            conversation.PatientId ==
                                patientId &&
                            conversation.IsActive
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

        private async Task<Guid>
            GetActivePatientIdAsync(
                Guid userId
            )
        {
            /*
             * Chatbot methods only need Patient.Id here. Avoid
             * loading the full Patient + User entity graph.
             */
            var patientId =
                await _context.Patients
                    .AsNoTracking()
                    .Where(
                        patient =>
                            patient.UserId ==
                                userId &&
                            patient.User.Role ==
                                RoleNames.Patient &&
                            patient.User.IsActive
                    )
                    .Select(
                        patient =>
                            (Guid?)patient.Id
                    )
                    .FirstOrDefaultAsync();

            if (patientId == null)
            {
                throw new UnauthorizedAccessException(
                    "Active Patient account required."
                );
            }

            return patientId.Value;
        }

        // =====================================================
        // PATIENT CONTEXT
        // =====================================================

        private async Task<string>
            BuildPatientContextAsync(
                Guid patientId
            )
        {
            /*
             * These queries are prompt-building reads only, so
             * they do not need change tracking.
             */
            var medications =
                await _context.Medications
                    .AsNoTracking()
                    .Include(
                        medication =>
                            medication.Schedules
                                .Where(
                                    schedule =>
                                        schedule.IsActive
                                )
                    )
                    .Where(
                        medication =>
                            medication.PatientId ==
                                patientId &&
                            medication.IsActive
                    )
                    .OrderBy(
                        medication =>
                            medication.Name
                    )
                    .ToListAsync();

            var allergies =
                await _context.Allergies
                    .AsNoTracking()
                    .Where(
                        allergy =>
                            allergy.PatientId ==
                                patientId
                    )
                    .OrderBy(
                        allergy =>
                            allergy.AllergyName
                    )
                    .Select(
                        allergy =>
                            allergy.AllergyName
                    )
                    .ToListAsync();

            var conditions =
                await _context.MedicalConditions
                    .AsNoTracking()
                    .Where(
                        condition =>
                            condition.PatientId ==
                                patientId
                    )
                    .OrderBy(
                        condition =>
                            condition.ConditionName
                    )
                    .Select(
                        condition =>
                            condition.ConditionName
                    )
                    .ToListAsync();

            var latestAssessment =
                await _context.SymptomAssessments
                    .AsNoTracking()
                    .Where(
                        assessment =>
                            assessment.PatientId ==
                                patientId
                    )
                    .OrderByDescending(
                        assessment =>
                            assessment.CreatedAt
                    )
                    .FirstOrDefaultAsync();

            var completedCollections =
                await _context.MedicationCollections
                    .AsNoTracking()
                    .Include(
                        collection =>
                            collection.Items
                    )
                    .Where(
                        collection =>
                            collection.PatientId ==
                                patientId &&
                            collection.Status ==
                                MedicationCollectionStatuses
                                    .Collected &&
                            collection.CollectedAt !=
                                null
                    )
                    .OrderByDescending(
                        collection =>
                            collection.CollectedAt
                    )
                    .ToListAsync();

            var builder =
                new StringBuilder();

            builder.AppendLine(
                "PHILALINK PATIENT CONTEXT"
            );

            builder.AppendLine();
            builder.AppendLine(
                "STORED PATIENT PROFILE"
            );

            builder.AppendLine(
                "The information in this section comes from the patient's stored PhilaLink profile."
            );

            builder.AppendLine(
                $"Active medications: {FormatMedicationList(medications)}"
            );

            builder.AppendLine(
                $"Allergies: {FormatStringList(allergies)}"
            );

            builder.AppendLine(
                $"Medical conditions: {FormatStringList(conditions)}"
            );

            builder.AppendLine();
            builder.AppendLine(
                "MEDICATION SUPPLY"
            );

            builder.AppendLine(
                "Medication supply values are estimates calculated by PhilaLink from recorded collections, dose size and active schedules."
            );

            if (medications.Count == 0)
            {
                builder.AppendLine(
                    "No active medications are recorded."
                );
            }
            else
            {
                var now =
                    DateTime.UtcNow;

                foreach (
                    var medication
                    in medications
                )
                {
                    builder.AppendLine(
                        BuildMedicationSupplyContext(
                            medication,
                            completedCollections,
                            now
                        )
                    );
                }
            }

            builder.AppendLine();
            builder.AppendLine(
                "LATEST SYMPTOM ASSESSMENT"
            );

            builder.AppendLine(
                "This section contains information the patient entered during their latest PhilaChatBot symptom assessment. " +
                "It may differ from the permanent stored patient profile. " +
                "Treat it as assessment-session information and do not claim that it changed the permanent profile."
            );

            if (latestAssessment == null)
            {
                builder.AppendLine(
                    "No symptom assessment is recorded."
                );
            }
            else
            {
                builder.AppendLine(
                    $"Assessment recorded at UTC: {latestAssessment.CreatedAt:O}"
                );

                builder.AppendLine(
                    $"Triage result: {latestAssessment.Result}"
                );

                builder.AppendLine(
                    $"Assessment recommendation: {latestAssessment.Recommendation}"
                );

                builder.AppendLine(
                    $"Assessment details JSON: {latestAssessment.SymptomsJson}"
                );
            }

            builder.AppendLine();
            builder.AppendLine(
                "CONTEXT RULES"
            );

            builder.AppendLine(
                "- When the patient asks about symptoms from their assessment, use the latest symptom assessment above."
            );

            builder.AppendLine(
                "- When the patient asks how much medication they have left, use the Medication Supply section above."
            );

            builder.AppendLine(
                "- Clearly describe medication supply as an estimate."
            );

            builder.AppendLine(
                "- Do not ask the patient for collection quantities or collection dates when PhilaLink already provides them above."
            );

            builder.AppendLine(
                "- If medication supply is unavailable because required data is missing, explain which recorded information is missing."
            );

            builder.AppendLine(
                "- Do not treat assessment-entered medications, allergies or conditions as permanent profile records unless they also appear in the stored profile section."
            );

            builder.AppendLine(
                "- Never claim that the symptom assessment establishes a confirmed diagnosis."
            );

            return builder
                .ToString()
                .Trim();
        }

        private static string FormatMedicationList(
            IReadOnlyCollection<Medication> medications
        )
        {
            if (medications.Count == 0)
            {
                return "None recorded";
            }

            return string.Join(
                "; ",
                medications.Select(
                    medication =>
                    {
                        var parts =
                            new[]
                            {
                                medication.Name,
                                medication.Dosage,
                                medication.Form
                            }
                            .Where(
                                value =>
                                    !string.IsNullOrWhiteSpace(
                                        value
                                    )
                            );

                        return string.Join(
                            " ",
                            parts
                        );
                    }
                )
            );
        }

        private static string FormatStringList(
            IReadOnlyCollection<string> values
        )
        {
            if (values.Count == 0)
            {
                return "None recorded";
            }

            return string.Join(
                ", ",
                values
            );
        }

        // =====================================================
        // MEDICATION SUPPLY CONTEXT
        // =====================================================

        private static string
            BuildMedicationSupplyContext(
                Medication medication,
                IReadOnlyList<MedicationCollection>
                    completedCollections,
                DateTime now
            )
        {
            var activeSchedules =
                medication.Schedules
                    .Where(
                        schedule =>
                            schedule.IsActive
                    )
                    .OrderBy(
                        schedule =>
                            schedule.TimeOfDay
                    )
                    .ToList();

            var latestCollection =
                completedCollections
                    .FirstOrDefault(
                        collection =>
                            collection.Items.Any(
                                item =>
                                    item.MedicationId ==
                                        medication.Id
                            )
                    );

            var medicationLabel =
                string.Join(
                    " ",
                    new[]
                    {
                        medication.Name,
                        medication.Dosage,
                        medication.Form
                    }
                    .Where(
                        value =>
                            !string.IsNullOrWhiteSpace(
                                value
                            )
                    )
                );

            if (
                latestCollection == null ||
                latestCollection.CollectedAt ==
                    null
            )
            {
                return
                    $"- {medicationLabel}: " +
                    "days remaining unavailable because no completed collection is recorded.";
            }

            var dispensedQuantity =
                latestCollection.Items
                    .Where(
                        item =>
                            item.MedicationId ==
                                medication.Id
                    )
                    .Sum(
                        item =>
                            item.Quantity
                    );

            if (
                medication.UnitsPerDose ==
                    null ||
                medication.UnitsPerDose <= 0
            )
            {
                return
                    $"- {medicationLabel}: " +
                    "days remaining unavailable because units per dose are missing. " +
                    $"Last dispensed quantity: {dispensedQuantity}.";
            }

            if (
                activeSchedules.Count == 0
            )
            {
                return
                    $"- {medicationLabel}: " +
                    "days remaining unavailable because no active dosing schedule is recorded. " +
                    $"Last dispensed quantity: {dispensedQuantity}.";
            }

            var collectedAt =
                latestCollection
                    .CollectedAt.Value;

            var scheduledDosesUsed =
                CountScheduledDoses(
                    collectedAt,
                    now,
                    activeSchedules
                );

            var estimatedUnitsUsed =
                scheduledDosesUsed *
                medication
                    .UnitsPerDose.Value;

            var estimatedRemaining =
                Math.Max(
                    0m,
                    dispensedQuantity -
                    estimatedUnitsUsed
                );

            var unitsPerDay =
                activeSchedules.Count *
                medication
                    .UnitsPerDose.Value;

            int? daysRemaining =
                null;

            if (unitsPerDay > 0)
            {
                daysRemaining =
                    (int)Math.Floor(
                        estimatedRemaining /
                        unitsPerDay
                    );
            }

            if (!daysRemaining.HasValue)
            {
                return
                    $"- {medicationLabel}: " +
                    "days remaining could not be calculated.";
            }

            return
                $"- {medicationLabel}: " +
                $"approximately {daysRemaining.Value} day(s) remaining; " +
                $"estimated remaining quantity {estimatedRemaining:0.##} unit(s); " +
                $"dispensed quantity {dispensedQuantity}; " +
                $"units per dose {medication.UnitsPerDose.Value:0.####}; " +
                $"doses per day {activeSchedules.Count}; " +
                $"last collected {collectedAt:yyyy-MM-dd}.";
        }

        private static int CountScheduledDoses(
            DateTime from,
            DateTime to,
            IReadOnlyList<MedicationSchedule>
                schedules
        )
        {
            if (
                schedules.Count == 0 ||
                to <= from
            )
            {
                return 0;
            }

            var count =
                0;

            var date =
                from.Date;

            var lastDate =
                to.Date;

            while (
                date <= lastDate
            )
            {
                foreach (
                    var schedule
                    in schedules
                )
                {
                    var occurrence =
                        date +
                        schedule.TimeOfDay;

                    if (
                        occurrence >
                            from &&
                        occurrence <=
                            to
                    )
                    {
                        count++;
                    }
                }

                date =
                    date.AddDays(1);
            }

            return count;
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
