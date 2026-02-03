using AutoMate_app.Models.Options;
using Microsoft.Extensions.Options;
using System.Text;
using System.Text.Json;

namespace AutoMate_app.Services
{
    public class ChatMessage
    {
        public string Role { get; set; }
        public string Content { get; set; }
    }

    public class ChatService
    {
        private readonly HttpClient _httpClient;
        private readonly GeminiOptions _gemini;

        public ChatService(HttpClient httpClient, IConfiguration config, IOptions<GeminiOptions> geminiOptions)
        {
            _httpClient = httpClient;
            _gemini = geminiOptions.Value;

        }

        public async Task<string?> GetChatResponseAsync(string userMessage, List<ChatMessage>? conversationHistory = null)
        {
            var systemPrompt = @"You are a helpful assistant for AutoMate_app, a roadside assistance platform.
                Help users with questions about car problems, service requests, how to use the app, or general automotive advice.
                Be friendly, concise, and helpful.";


            var messages = new List<object>();

            // Add system message as first user message (Gemini doesn't have separate system role)
            messages.Add(new
            {
                role = "user",
                parts = new[] { new { text = systemPrompt } }
            });

            // Add conversation history if provided
            if (conversationHistory != null && conversationHistory.Any())
            {
                foreach (var msg in conversationHistory)
                {
                    messages.Add(new
                    {
                        role = msg.Role == "assistant" ? "model" : "user", // Gemini uses "model" not "assistant"
                        parts = new[] { new { text = msg.Content } }
                    });
                }
            }

            // Add current user message
            messages.Add(new
            {
                role = "user",
                parts = new[] { new { text = userMessage } }
            });

            var body = new
            {
                contents = messages,
                generationConfig = new
                {
                    temperature = 0.7,
                    topK = 40,
                    topP = 0.95,
                    maxOutputTokens = 1024
                }
            };

            try
            {
                var url = $"https://generativelanguage.googleapis.com/v1beta/models/{_gemini.Model}:generateContent?key={_gemini.ApiKey}";
                var json = JsonSerializer.Serialize(body);
                var response = await _httpClient.PostAsync(url,
                    new StringContent(json, Encoding.UTF8, "application/json"));

                response.EnsureSuccessStatusCode();
                var responseJson = await response.Content.ReadAsStringAsync();

                using var doc = JsonDocument.Parse(responseJson);

                // Check if there's an error in the response
                if (doc.RootElement.TryGetProperty("error", out var errorElement))
                {
                    var errorMessage = errorElement.GetProperty("message").GetString();
                    throw new Exception($"Gemini API error: {errorMessage}");
                }

                // Check if candidates array exists and has items
                if (!doc.RootElement.TryGetProperty("candidates", out var candidates) ||
                    candidates.GetArrayLength() == 0)
                {
                    throw new Exception("No candidates in Gemini response");
                }

                var content = doc.RootElement
                    .GetProperty("candidates")[0]
                    .GetProperty("content")
                    .GetProperty("parts")[0]
                    .GetProperty("text")
                    .GetString();

                // Clean up markdown code blocks if present
                if (!string.IsNullOrWhiteSpace(content))
                {
                    content = content
                        .Replace("```json", "", StringComparison.OrdinalIgnoreCase)
                        .Replace("```text", "", StringComparison.OrdinalIgnoreCase)
                        .Replace("```plaintext", "", StringComparison.OrdinalIgnoreCase)
                        .Replace("```", "", StringComparison.OrdinalIgnoreCase)
                        .Trim();
                }

                return content;
            }
            catch (HttpRequestException ex)
            {
                throw new Exception($"Failed to communicate with Gemini API: {ex.Message}", ex);
            }
            catch (JsonException ex)
            {
                throw new Exception($"Failed to parse Gemini response: {ex.Message}", ex);
            }
            catch (Exception ex)
            {
                throw new Exception($"Error getting chat response: {ex.Message}", ex);
            }
        }
    }
}