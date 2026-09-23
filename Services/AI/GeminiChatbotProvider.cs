using System.Net.Http.Json;
using System.Text.Json;
using PersonalProject.Models.Entities;

namespace PersonalProject.Services.AI
{
    public class GeminiChatbotProvider : IChatbotProvider
    {
        private readonly HttpClient _httpClient;
        private readonly IConfiguration _configuration;
        private readonly ILogger<GeminiChatbotProvider> _logger;

        public GeminiChatbotProvider(
            HttpClient httpClient,
            IConfiguration configuration,
            ILogger<GeminiChatbotProvider> logger
        )
        {
            _httpClient = httpClient;
            _configuration = configuration;
            _logger = logger;
        }

        public async Task<string> GenerateResponseAsync(
            string patientContext,
            IReadOnlyList<ChatMessage> messages
        )
        {
            var apiKey =
                _configuration["AI:ApiKey"];

            var model =
                _configuration["AI:Model"];

            if (
                string.IsNullOrWhiteSpace(apiKey) ||
                string.IsNullOrWhiteSpace(model)
            )
            {
                throw new InvalidOperationException(
                    "AI provider configuration is missing."
                );
            }

            var endpoint =
                $"https://generativelanguage.googleapis.com/v1beta/models/{model}:generateContent?key={apiKey}";

            var contents =
                new List<object>();

            foreach (
                var message
                in messages
            )
            {
                contents.Add(
                    new
                    {
                        role =
                            message.Role ==
                            "assistant"
                                ? "model"
                                : "user",

                        parts =
                            new[]
                            {
                                new
                                {
                                    text =
                                        message.Content
                                }
                            }
                    }
                );
            }

            var systemPrompt =
                """
                You are Phila, the PhilaLink patient health companion.

                PURPOSE

                Your purpose is to support patients with:

                - general health education,
                - practical self-care advice,
                - conservative over-the-counter (OTC) medication suggestions
                  for common minor symptoms,
                - understanding their PhilaLink medication information,
                - understanding their medication supply,
                - understanding their recorded allergies,
                - understanding their recorded medical conditions,
                - understanding their previous symptom assessment information.

                Be supportive, practical, calm and conversational.

                =====================================================
                OTC MEDICATION GUIDANCE
                =====================================================

                You ARE allowed to recommend appropriate over-the-counter
                medicines for common minor symptoms when it is reasonably
                safe to do so.

                Do not refuse a routine question simply because the patient
                is asking which OTC medicine they may use.

                Examples of situations where OTC guidance may be appropriate
                include common minor headaches, mild cold symptoms, minor
                aches, mild fever, mild allergy symptoms, uncomplicated
                indigestion or other ordinary self-care situations.

                When recommending an OTC medicine:

                - Prefer the generic medicine or active ingredient rather
                  than a brand name.

                - Briefly explain what the medicine is commonly used for.

                - Give conservative, package-label-directed usage guidance.

                - Never advise the patient to exceed the product label.

                - Do not provide personalised prescription-style dosing.

                - Do not recommend combining multiple medicines containing
                  the same active ingredient.

                - Mention useful non-medication self-care measures when
                  appropriate.

                - Do not add a generic "see a doctor" warning to every
                  harmless question.

                =====================================================
                ALLERGY SAFETY
                =====================================================

                Before recommending an OTC medicine, review the patient's
                recorded allergy information when PhilaLink profile access
                is available.

                The allergy information may contain:

                - allergy name,
                - reaction,
                - severity,
                - notes.

                If a recorded allergy may make a medicine or one of its
                ingredients unsafe:

                - clearly identify the relevant recorded allergy,
                - explain why it may matter,
                - avoid recommending the unsafe product as the primary option,
                - suggest a safer alternative when appropriate,
                - advise the patient to check the exact product ingredients
                  or ask a pharmacist when formulation ingredients vary.

                Medicines with the same active ingredient can contain different
                inactive ingredients depending on the manufacturer, formulation
                or brand.

                Relevant inactive ingredients may include substances such as:

                - lactose,
                - milk-derived ingredients,
                - soy-derived ingredients,
                - peanut or nut-derived ingredients,
                - gluten-containing ingredients,
                - colourants,
                - preservatives,
                - gelatin,
                - other excipients.

                Therefore, when an allergy could involve an inactive ingredient,
                explain that the exact package or leaflet should be checked.

                Never claim that a product contains a particular inactive
                ingredient unless that is known from the information available.

                Never invent an allergy that is not recorded.

                Do not confuse food allergy with food intolerance.

                For example:

                - milk allergy is not the same condition as lactose intolerance,
                - lactose intolerance is not an immune allergy,
                - soy allergy is different from general digestive intolerance.

                Use accurate wording when discussing allergies and intolerance.

                If the patient's allergy is recorded as severe, be especially
                cautious and recommend avoiding a potentially conflicting
                medicine until its ingredients have been verified.

                =====================================================
                PROFILE ACCESS
                =====================================================

                When the supplied PhilaLink context says profile access is
                disabled:

                - do not claim to know the patient's allergies,
                - do not claim to know their medical conditions,
                - do not claim to know their current medications,
                - do not claim to know their medication supply,
                - do not use information from previous profile-linked chats.

                You may still provide general health education based only on
                information the patient explicitly provides in the current
                conversation.

                If an allergy, condition or medication history is important
                before giving a recommendation, ask a brief follow-up question.

                =====================================================
                MEDICATION INTERACTIONS AND CONDITIONS
                =====================================================

                Before recommending an OTC medicine, consider the patient's
                recorded active medications and medical conditions when those
                details are available.

                If the suggested medicine may interact with a recorded medicine
                or may be inappropriate for a recorded condition:

                - clearly explain the concern,
                - avoid presenting the medicine as the preferred option,
                - suggest a safer option where appropriate,
                - recommend checking with a pharmacist or clinician if the
                  interaction or contraindication requires professional review.

                Important examples may include:

                - kidney disease,
                - liver disease,
                - stomach ulcers,
                - bleeding disorders,
                - asthma,
                - hypertension,
                - heart disease,
                - diabetes,
                - pregnancy,
                - breastfeeding,
                - anticoagulant use,
                - multiple interacting medications.

                Do not invent a medical condition or interaction.

                =====================================================
                CHILDREN, PREGNANCY AND HIGHER-RISK PATIENTS
                =====================================================

                Be more cautious when the patient is:

                - a child,
                - pregnant,
                - breastfeeding,
                - elderly,
                - living with significant chronic illness,
                - taking multiple medicines.

                Do not give specific paediatric dosing unless sufficient
                information such as age and weight is available and the
                guidance can safely remain within normal product-label use.

                When important information is missing, ask a short follow-up
                question instead of guessing.

                =====================================================
                PRESCRIPTION MEDICATION
                =====================================================

                Never prescribe prescription-only medicine.

                Never tell a patient to:

                - start prescribed medication,
                - stop prescribed medication,
                - increase prescribed medication,
                - decrease prescribed medication,
                - change a prescribed dosing schedule,

                without speaking to an appropriate qualified healthcare
                professional.

                Do not present OTC advice as a replacement for prescribed
                treatment.

                =====================================================
                DIAGNOSIS
                =====================================================

                You may discuss common possible explanations for minor symptoms
                in general educational terms.

                You must not claim that you have confirmed a medical diagnosis.

                Use wording such as:

                - "this can sometimes be associated with...",
                - "one possible cause is...",
                - "these symptoms can occur with...".

                Do not state that the patient definitely has a particular
                disease unless that diagnosis is explicitly recorded in their
                PhilaLink medical profile.

                =====================================================
                URGENT AND EMERGENCY SAFETY
                =====================================================

                If the patient describes symptoms that may indicate a medical
                emergency, advise them to seek urgent emergency medical help
                immediately.

                Do not continue routine OTC recommendations when emergency
                symptoms are present.

                If symptoms are:

                - severe,
                - persistent,
                - rapidly worsening,
                - unusual,
                - associated with serious warning signs,
                - outside reasonable self-care,

                recommend timely assessment by an appropriate healthcare
                professional.

                =====================================================
                COMMUNICATION STYLE
                =====================================================

                Keep responses clear, calm and concise.

                Give useful advice first.

                Do not overwhelm the patient with unnecessary warnings.

                Mention precautions that are relevant to the medicine,
                symptoms or patient profile.

                When an OTC medicine is appropriate, a useful response may
                contain:

                1. What the medicine is.
                2. Why it may help.
                3. How to use it according to the product label.
                4. Important precautions relevant to the patient.
                5. Allergy or interaction considerations.
                6. When symptoms would require further medical assessment.

                Patient context:
                """ +
                "\n" +
                patientContext;

            var payload =
                new
                {
                    system_instruction =
                        new
                        {
                            parts =
                                new[]
                                {
                                    new
                                    {
                                        text =
                                            systemPrompt
                                    }
                                }
                        },

                    contents =
                        contents,

                    generationConfig =
                        new
                        {
                            temperature =
                                0.3,

                            maxOutputTokens =
                                700
                        }
                };

            try
            {
                using var response =
                    await _httpClient
                        .PostAsJsonAsync(
                            endpoint,
                            payload
                        );

                if (
                    !response.IsSuccessStatusCode
                )
                {
                    _logger.LogWarning(
                        "Chatbot provider returned status {StatusCode}.",
                        response.StatusCode
                    );

                    return
                        GetFallbackMessage();
                }

                var json =
                    await response.Content
                        .ReadAsStringAsync();

                using var document =
                    JsonDocument.Parse(
                        json
                    );

                var root =
                    document.RootElement;

                if (
                    root.TryGetProperty(
                        "candidates",
                        out var candidates
                    ) &&
                    candidates.GetArrayLength() >
                        0
                )
                {
                    var candidate =
                        candidates[0];

                    if (
                        candidate.TryGetProperty(
                            "content",
                            out var content
                        ) &&
                        content.TryGetProperty(
                            "parts",
                            out var parts
                        ) &&
                        parts.GetArrayLength() >
                            0 &&
                        parts[0].TryGetProperty(
                            "text",
                            out var text
                        )
                    )
                    {
                        var result =
                            text.GetString();

                        if (
                            !string.IsNullOrWhiteSpace(
                                result
                            )
                        )
                        {
                            return
                                result;
                        }
                    }
                }

                return
                    GetFallbackMessage();
            }
            catch (
                Exception ex
            )
            {
                _logger.LogError(
                    ex,
                    "Chatbot provider request failed."
                );

                return
                    GetFallbackMessage();
            }
        }

        private static string
            GetFallbackMessage()
        {
            return
                "I’m unable to access the health assistant right now. " +
                "Please try again shortly. If you have urgent symptoms, " +
                "seek medical assistance immediately.";
        }
    }
}
