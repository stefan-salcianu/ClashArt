using System.Text;
using System.Text.Json;
using ClashArt.Services.Gemini;

public class ContentModerator
{
    private readonly HttpClient _httpClient;
    private readonly string _apiKey;

    // ✅ MODELUL CÂȘTIGĂTOR
    private const string ModelUrl = "https://generativelanguage.googleapis.com/v1beta/models/gemini-flash-latest:generateContent";

    public ContentModerator(HttpClient httpClient, IConfiguration configuration)
    {
        _httpClient = httpClient;
        _apiKey = configuration["Gemini:ApiKey"];
    }

    public async Task<bool> IsTextSafe(string userComment)
    {
        // 1. Siguranță: Dacă cheia nu e citită (ex: ești pe alt PC), lasă site-ul să meargă.
        if (string.IsNullOrEmpty(_apiKey) || _apiKey.Contains("AICI_VA_FI"))
        {
            return true;
        }

        // 2. Construim JSON-ul (Metoda Anonimă - s-a dovedit cea mai robustă la tine)
        var requestData = new
        {
            contents = new[]
            {
                new
                {
                    parts = new[]
                    {
                        new { text = $"Role: Content Moderator. Task: Check text for toxicity. Text: \"{userComment}\". Output ONLY: 'GOOD' (safe) or 'BAD' (toxic)." }
                    }
                }
            }
        };

        var options = new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase };
        var json = JsonSerializer.Serialize(requestData, options);
        var content = new StringContent(json, Encoding.UTF8, "application/json");

        try
        {
            // 3. Trimitem cererea
            var response = await _httpClient.PostAsync($"{ModelUrl}?key={_apiKey}", content);

            // Dacă Google dă eroare (server picat, limită atinsă), nu blocăm utilizatorul.
            if (!response.IsSuccessStatusCode) return true;

            var jsonResponse = await response.Content.ReadAsStringAsync();
            using var doc = JsonDocument.Parse(jsonResponse);

            // 4. Citim răspunsul
            if (doc.RootElement.TryGetProperty("candidates", out var candidates) && candidates.GetArrayLength() > 0)
            {
                var text = candidates[0].GetProperty("content").GetProperty("parts")[0].GetProperty("text").GetString()?.Trim().ToUpper();

                // Dacă AI-ul zice GOOD, e safe. Orice altceva e considerat toxic.
                return text != null && text.Contains("GOOD");
            }

            // Dacă răspunsul e ciudat, lăsăm să treacă (Fail Open)
            return true;
        }
        catch
        {
            // Orice eroare neprevăzută -> Safe (ca să nu crape site-ul la prezentare)
            return true;
        }
    }
}