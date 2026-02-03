using AutoMate_app.Models.Options;
using Microsoft.Extensions.Options;
using System.Text;
using System.Text.Json;
namespace AutoMate_app.Services
{


    public class AiAdvisorResult
    {
        public string ServiceType { get; set; }
        public List<string> PossibleReasons { get; set; } = new();
        public string Urgency { get; set; }          // "High" | "Medium" | "Low"
        public bool RecommendTowing { get; set; }
    }
    public class GeminiAdvisorService
    {
        private readonly HttpClient _httpClient;

        private readonly GeminiOptions _gemini;
        private readonly ILogger<GeminiAdvisorService> _logger;

        public GeminiAdvisorService(
    HttpClient httpClient,
    IOptions<GeminiOptions> geminiOptions,
    ILogger<GeminiAdvisorService> logger)
        {
            _httpClient = httpClient;
            _gemini = geminiOptions.Value;
            _logger = logger;
        }

        public async Task<AiAdvisorResult?> GetAdviceAsync(string problemDescription)
        {
            var systemPrompt = @"You are an automotive triage assistant.
                                Return ONLY valid JSON (no extra text) in this exact format:
                                {
                                  ""serviceType"": string,
                                  ""possibleReasons"": string[],
                                  ""urgency"": ""High"" | ""Medium"" | ""Low"",
                                  ""recommendTowing"": true | false
                                }";

            var body = new
            {
                contents = new[]
                {
                new {
                    role = "user",
                    parts = new[] {
                        new { text = systemPrompt + "\n\nUser problem: " + problemDescription }
                    }
                }
            }
            };

            var url = $"https://generativelanguage.googleapis.com/v1beta/models/{_gemini.Model}:generateContent?key={_gemini.ApiKey}";

            var json = JsonSerializer.Serialize(body);

            var response = await _httpClient.PostAsync(url,
                new StringContent(json, Encoding.UTF8, "application/json"));

            response.EnsureSuccessStatusCode();
            var responseJson = await response.Content.ReadAsStringAsync();




            using var doc = JsonDocument.Parse(responseJson);
            var content = doc.RootElement
                .GetProperty("candidates")[0]
                .GetProperty("content")
                .GetProperty("parts")[0]
                .GetProperty("text")
                .GetString();


            if (content == null)
            {
                _logger.LogError("Gemini response content is null.");
                return null;
            }

            content = content
                .Replace("```json", "")
                .Replace("```", "")
                .Trim();

            return JsonSerializer.Deserialize<AiAdvisorResult>(content,
                new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
        }
    }
}
