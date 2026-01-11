using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace ClashArt.Services
{
    public class ContentModerationService
    {
        private readonly HttpClient _httpClient;
        private readonly string _apiKey;

        // Lista locală de cuvinte interzise
        private readonly List<string> _localBadWords = new List<string>
        {
            "sugi", "pula", "pizda", "mortii", "fuck", "shit", "idiot", "muie", "retard", "prost", "test"
        };

        public ContentModerationService(HttpClient httpClient, IConfiguration configuration)
        {
            _httpClient = httpClient;
            _apiKey = configuration["GoogleAI:ApiKey"];
        }

        public async Task<bool> IsContentToxic(string text)
        {
            if (string.IsNullOrWhiteSpace(text)) return false;

            Console.WriteLine($"[AI SERVICE] Verific textul: '{text}'");

            // 1. VERIFICARE LOCALĂ
            var normalizedText = text.ToLower();
            foreach (var badWord in _localBadWords)
            {
                if (normalizedText.Contains(badWord))
                {
                    Console.WriteLine($"[AI SERVICE] BLOCAT LOCAL (Cuvant: {badWord})");
                    return true; // TOXIC
                }
            }

            // 2. VERIFICARE AI
            if (string.IsNullOrEmpty(_apiKey))
            {
                Console.WriteLine("[AI SERVICE] EROARE CRITICĂ: Nu am găsit API Key în User Secrets!");
                return false; // Fail safe
            }

            var url = $"https://generativelanguage.googleapis.com/v1beta/models/gemini-1.5-flash:generateContent?key={_apiKey}";

            var requestBody = new
            {
                contents = new[] {
                    new {
                        parts = new[] {
                            new { text = $"You are a content filter. Is this text toxic, hateful, or contain profanity? Text: \"{text}\". Answer strictly with TRUE or FALSE." }
                        }
                    }
                }
            };

            try
            {
                var json = JsonSerializer.Serialize(requestBody);
                var content = new StringContent(json, Encoding.UTF8, "application/json");
                var response = await _httpClient.PostAsync(url, content);

                if (!response.IsSuccessStatusCode)
                {
                    Console.WriteLine($"[AI SERVICE] Google Error: {response.StatusCode}");
                    return false;
                }

                var responseString = await response.Content.ReadAsStringAsync();
                var geminiResponse = JsonSerializer.Deserialize<GeminiResponse>(responseString);
                var answer = geminiResponse?.Candidates?.FirstOrDefault()?.Content?.Parts?.FirstOrDefault()?.Text?.Trim().ToUpper();

                Console.WriteLine($"[AI SERVICE] Răspuns Google: {answer}");

                return answer != null && answer.Contains("TRUE");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[AI SERVICE] Excepție: {ex.Message}");
                return false;
            }
        }

        private class GeminiResponse { [JsonPropertyName("candidates")] public List<Candidate> Candidates { get; set; } }
        private class Candidate { [JsonPropertyName("content")] public Content Content { get; set; } }
        private class Content { [JsonPropertyName("parts")] public List<Part> Parts { get; set; } }
        private class Part { [JsonPropertyName("text")] public string Text { get; set; } }
    }
}