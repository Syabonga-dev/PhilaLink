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

            var contents = new List<object>();

            foreach (var message in messages)
            {
                contents.Add(
                    new
                    {
                        role =
                            message.Role == "assistant"
                                ? "model"
                                : "user",

                        parts = new[]
                        {
                            new
                            {
                                text = message.Content
                            }
                        }
                    }
                );
            }

            var systemPrompt =
                """
                You are Phila, the PhilaLink patient health assistant.

                You provide general health education and help patients understand
                their own PhilaLink information.

                You must not claim to provide a confirmed medical diagnosis.

                Never tell a patient to stop, start, increase or decrease prescribed
                medication without speaking to a qualified healthcare professional.

                If the patient describes a possible medical emergency, advise them
                to seek urgent emergency medical help immediately.

                Keep answers clear, calm and concise.

                Patient context:
                """ +
                "\n" +
                patientContext;

            var payload = new
            {
                system_instruction = new
                {
                    parts = new[]
                    {
                        new
                        {
                            text = systemPrompt
                        }
                    }
                },

                contents = contents,

                generationConfig = new
                {
                    temperature = 0.3,
                    maxOutputTokens = 700
                }
            };

            try
            {
                using var response =
                    await _httpClient.PostAsJsonAsync(
                        endpoint,
                        payload
                    );

                if (!response.IsSuccessStatusCode)
                {
                    _logger.LogWarning(
                        "Chatbot provider returned status {StatusCode}.",
                        response.StatusCode
                    );

                    return GetFallbackMessage();
                }

                var json =
                    await response.Content
                        .ReadAsStringAsync();

                using var document =
                    JsonDocument.Parse(json);

                var root =
                    document.RootElement;

                if (
                    root.TryGetProperty(
                        "candidates",
                        out var candidates
                    ) &&
                    candidates.GetArrayLength() > 0
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
                        parts.GetArrayLength() > 0 &&
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
                            return result;
                        }
                    }
                }

                return GetFallbackMessage();
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "Chatbot provider request failed."
                );

                return GetFallbackMessage();
            }
        }

        private static string GetFallbackMessage()
        {
            return
                "I’m unable to access the health assistant right now. " +
                "Please try again shortly. If you have urgent symptoms, " +
                "seek medical assistance immediately.";
        }
    }
}