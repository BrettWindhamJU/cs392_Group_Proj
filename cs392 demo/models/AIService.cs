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

            // Empty strings in appsettings should not block fallback to user secrets/env vars.
            var configuredApiKey = aiSettings["ApiKey"];
            if (string.IsNullOrWhiteSpace(configuredApiKey))
                configuredApiKey = config["GOOGLE_API_KEY"];
            if (string.IsNullOrWhiteSpace(configuredApiKey))
                configuredApiKey = config["GEMINI_API_KEY"];

            _apiKey = !string.IsNullOrWhiteSpace(configuredApiKey)
                ? configuredApiKey
                : throw new InvalidOperationException("Gemini API key is not configured. Set AISettings:ApiKey, GOOGLE_API_KEY, or GEMINI_API_KEY.");
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


        private static string BuildPromptSupplier(string userMessage, string? dataContext = null)
        {
            var contextSection = string.IsNullOrWhiteSpace(dataContext)
                ? ""
                : $@"
Data context from your system:
{dataContext}

Use this data as the source of truth for supplier-related answers.
If the requested information is not present in the context, say that clearly.
";

            return $@"
You are a supplier assistant for an inventory app.

Rules:
- Answer supplier-related questions across contacts, addresses, status, categories, payment terms, lead times, minimum orders, reliability, and catalog items.
- If the question is not supplier-related, reply that you can only help with supplier information.
- Use only the provided supplier data context for factual claims.
- Never invent supplier records or values.
- Be clear and well-structured.
- Use numbered or bulleted lists when helpful.
- Avoid unnecessary repetition.
- Avoid Markdown symbols (*, **, #).
- Keep responses concise but informative.
{contextSection}
User question:
{userMessage}
";
        }
        private static string BuildPromptAnalytics(string userMessage, string? dataContext = null)
        {
            var contextSection = string.IsNullOrWhiteSpace(dataContext)
                ? ""
                : $@"
Data context from your system:
{dataContext}

";

            return $@"
@""Return ONLY valid JSON.

DO NOT include:
- markdown
- ```json
- explanations
- comments

If unsure, return an empty object {{}}.

Format:
{{
  """"stockId"""": [
    {{ """"date"""": number, """"amount"""": number }}
  ]
}}"",
{contextSection}
User question:
{userMessage}
";
        }



        public async Task<string> SendPromptAsync(string message)
        {
            if (string.IsNullOrWhiteSpace(message))
                throw new ArgumentNullException(nameof(message));

            var prompt = BuildPromptSupplier(message);

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

        public async Task<string> SendPromptWithContextAsync(string message, string dataContext, string type)
        {
            if (string.IsNullOrWhiteSpace(message))
                throw new ArgumentNullException(nameof(message));
            
            string prompt;
            if (type == null)
            {
                _logger.LogInformation("calling BuildPromptSupplier with {Message1} and {Message2}", message.ToString(), message.ToString());
                prompt = BuildPromptSupplier(message, dataContext);
                _logger.LogInformation("BuildPromptSupplier called with response {Prompt}", prompt.ToString());

            }
            else
            {
                _logger.LogInformation("calling BuildPromptAnalytics with {Message1} and {Message2}", message.ToString(), message.ToString());
                prompt = BuildPromptAnalytics(message, dataContext);
                _logger.LogInformation("BuildPromptAnalytics called with response {Prompt}", prompt.ToString());
            }
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