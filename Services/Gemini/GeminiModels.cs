namespace ClashArt.Services.Gemini
{
    // Structura cererii pe care o trimitem noi
    public class GeminiRequest
    {
        public Content[] contents { get; set; }
    }

    public class Content
    {
        public Part[] parts { get; set; }
    }

    public class Part
    {
        public string text { get; set; }
    }

    // Structura răspunsului pe care îl primim
    public class GeminiResponse
    {
        public Candidate[] candidates { get; set; }
    }

    public class Candidate
    {
        public Content content { get; set; }
    }
}