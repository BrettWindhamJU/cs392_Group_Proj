using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using System.Net;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace cs392_demo.models
{
    public class AIService
    {
        private readonly HttpClient _httpClient;
        private readonly ILogger<AIService> _logger;
        private readonly string _apiKey;
        private readonly string _model;

        public AIService(HttpClient httpClient, IConfiguration config, ILogger<AIService> logger)
        {
            _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));

            var aiSettings = config.GetSection("AISettings");
            _apiKey = aiSettings["ApiKey"]
                ?? config["GOOGLE_API_KEY"]
                ?? config["GEMINI_API_KEY"]
                ?? throw new InvalidOperationException("Gemini API key is not configured. Set AISettings:ApiKey, GOOGLE_API_KEY, or GEMINI_API_KEY.");
            _model = aiSettings["Model"] ?? "gemini-2.5-flash";

            var baseAddress = aiSettings["BaseAddress"]
                ?? aiSettings["BaseUrl"]
                ?? "https://generativelanguage.googleapis.com/v1beta/";
            _httpClient.BaseAddress = new Uri(baseAddress);
        }

        // 🔍 Extract FULL response (FIXED)
        private string ExtractGeminiText(string respText)
        {
            try
            {
                using var doc = JsonDocument.Parse(respText);

                if (doc.RootElement.TryGetProperty("candidates", out var candidates) &&
                    candidates.GetArrayLength() > 0)
                {
                    var candidate = candidates[0];

                    // Log finish reason for debugging
                    if (candidate.TryGetProperty("finishReason", out var reason))
                        _logger.LogInformation("Finish Reason: {Reason}", reason.ToString());

                    var parts = candidate.GetProperty("content").GetProperty("parts");
                    var fullText = new StringBuilder();

                    foreach (var part in parts.EnumerateArray())
                    {
                        if (part.TryGetProperty("text", out var textElement))
                        {
                            fullText.Append(textElement.GetString());
                        }
                    }

                    var result = fullText.ToString();
                    return string.IsNullOrWhiteSpace(result) ? "No clear response generated." : CleanResponse(result);
                }

                _logger.LogError("Unexpected Gemini response: {Response}", respText);
                return "No response from AI.";
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error parsing Gemini response");
                return "Error processing AI response.";
            }
        }

        // ✅ Clean Markdown symbols for readability
        private string CleanResponse(string raw)
        {
            if (string.IsNullOrWhiteSpace(raw))
                return raw;

            var cleaned = raw.Replace("###", "")
                             .Replace("**", "")
                             .Replace("*", "")
                             .Replace("\r\n", "\n")
                             .Replace("\r", "\n")
                             .Trim();

            var lines = cleaned
                .Split('\n')
                .Select(l => l.Trim())
                .ToList();

            // Keep paragraph breaks but avoid large vertical gaps.
            var normalizedLines = new List<string>(lines.Count);
            var previousBlank = false;
            foreach (var line in lines)
            {
                var isBlank = string.IsNullOrWhiteSpace(line);
                if (isBlank && previousBlank)
                {
                    continue;
                }

                normalizedLines.Add(line);
                previousBlank = isBlank;
            }

            cleaned = string.Join("\n", normalizedLines).Trim();

            return cleaned;
        }


        private static string BuildPrompt(string userMessage, string? dataContext = null)
        {
            var contextSection = string.IsNullOrWhiteSpace(dataContext)
                ? ""
                : $@"
Data context from your system:
{dataContext}

Use this data when answering supplier-related questions. If the requested information is not present in the context, say that clearly.
";

            return $@"
You are a helpful AI assistant.

Rules:
- Be clear and well-structured
- Provide reasonably detailed answers
- Use numbered or bulleted lists if appropriate
- Avoid unnecessary repetition
- Avoid Markdown symbols (*, **, #)
- Dont make the Response too long, but be informative
{contextSection}
User question:
{userMessage}
";
        }

        // 🚀 SIMPLE CHAT
        public async Task<string> SendPromptAsync(string message)
        {
            if (string.IsNullOrWhiteSpace(message))
                throw new ArgumentNullException(nameof(message));

            var prompt = BuildPrompt(message);

            var payload = new
            {
                contents = new[]
                {
                    new
                    {
                        role = "user",
                        parts = new[]
                        {
                            new { text = prompt }
                        }
                    }
                },
                generationConfig = new
                {
                    temperature = 0.3,
                    topP = 0.8,
                    topK = 40,
                    maxOutputTokens = 1024
                }
            };

            return await SendRequestAsync(payload);
        }

        public async Task<string> SendPromptWithContextAsync(string message, string dataContext)
        {
            if (string.IsNullOrWhiteSpace(message))
                throw new ArgumentNullException(nameof(message));

            var prompt = BuildPrompt(message, dataContext);

            var payload = new
            {
                contents = new[]
                {
                    new
                    {
                        role = "user",
                        parts = new[]
                        {
                            new { text = prompt }
                        }
                    }
                },
                generationConfig = new
                {
                    temperature = 0.2,
                    topP = 0.8,
                    topK = 40,
                    maxOutputTokens = 1024
                }
            };

            return await SendRequestAsync(payload);
        }

        // 💬 CHAT WITH HISTORY
        public async Task<string> SendChatAsync(List<(string Role, string Message)> messages)
        {
            if (messages == null || messages.Count == 0)
                throw new ArgumentException("Conversation cannot be empty.");

            var contents = new List<object>();
            foreach (var msg in messages)
            {
                contents.Add(new
                {
                    role = msg.Role.ToLower(),
                    parts = new[]
                    {
                        new { text = msg.Message }
                    }
                });
            }

            var payload = new
            {
                contents = contents,
                generationConfig = new
                {
                    temperature = 0.3,
                    topP = 0.8,
                    topK = 40,
                    maxOutputTokens = 1024
                }
            };

            return await SendRequestAsync(payload);
        }

        // 🔁 Shared request handler
        private async Task<string> SendRequestAsync(object payload)
        {
            var json = JsonSerializer.Serialize(payload);
            var endpoint = $"models/{_model}:generateContent?key={_apiKey}";

            using var content = new StringContent(json, Encoding.UTF8, "application/json");
            using var response = await _httpClient.PostAsync(endpoint, content);
            var responseText = await response.Content.ReadAsStringAsync();

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogError("Gemini API error: {Status} - {Response}", response.StatusCode, responseText);

                if (response.StatusCode == HttpStatusCode.Forbidden)
                {
                    throw new InvalidOperationException("Gemini API returned 403 Forbidden. Verify that the key is valid and Generative Language API is enabled.");
                }

                throw new InvalidOperationException($"Gemini API error: {response.StatusCode}");
            }

            return ExtractGeminiText(responseText);
        }
    }
}