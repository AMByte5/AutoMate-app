namespace AutoMate_app.Models.Options
{
    public class GeminiOptions
    {
        public const string SectionName = "Gemini";
        public string ApiKey { get; set; } = string.Empty;
        public string Model { get; set; } = "gemini-2.5-flash";
    }
}
