using System.Text;
using System.Text.Json;
using DashboardAPI.Models;  // ← AJOUT : AiResponse + DatasetRequest

namespace DashboardAPI.Services  // ← AJOUT : namespace
{
    public class AiService
    {
        private readonly HttpClient    _httpClient;
        private readonly IConfiguration _config;

        public AiService(HttpClient httpClient, IConfiguration config)
        {
            _httpClient = httpClient;
            _config     = config;
        }

        public async Task<AiResponse> AnalyzeAsync(DatasetRequest dataset)
        {
            var apiKey = _config["Gemini:ApiKey"];
            var prompt = $@"
        You are a strict JSON generator.

        IMPORTANT:
        - Return ONLY valid JSON
        - No text
        - No markdown
        - No explanation

        Schema:
        {{
        ""insights"": [""string""],
        ""recommendations"": [""string""]
        }}

        Dataset:
        File: {dataset.FileName}
        Columns: {dataset.Columns}
        Rows: {dataset.Rows}
        ";

            var body = new
            {
                contents = new[]
                {
                    new { parts = new[] { new { text = prompt } } }
                }
            };

            var request = new HttpRequestMessage(
                HttpMethod.Post,
                $"https://generativelanguage.googleapis.com/v1/models/gemini-2.5-flash:generateContent?key={apiKey}"
            );
            request.Content = new StringContent(
                JsonSerializer.Serialize(body),
                Encoding.UTF8,
                "application/json"
            );

            var response = await _httpClient.SendAsync(request);
            var json     = await response.Content.ReadAsStringAsync();

            using var doc  = JsonDocument.Parse(json);
            var root = doc.RootElement;

            if (!root.TryGetProperty("candidates", out var candidates))
                throw new Exception("Gemini error: " + json);

            var text = candidates[0]
                .GetProperty("content")
                .GetProperty("parts")[0]
                .GetProperty("text")
                .GetString();

            if (string.IsNullOrEmpty(text))
                throw new Exception("Empty AI response");

            text = text.Replace("```json", "").Replace("```", "").Trim();

            var result = JsonSerializer.Deserialize<AiResponse>(text,
                new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

            return result ?? throw new Exception("AI returned null JSON: " + text);
        }
    }
}