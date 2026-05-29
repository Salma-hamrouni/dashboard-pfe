using Microsoft.AspNetCore.Mvc;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;

namespace DashboardAPI.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AiTestController : ControllerBase
    {
        private readonly HttpClient _httpClient;
        private readonly IConfiguration _config;

        public AiTestController(IConfiguration config)
        {
            _config = config;
            _httpClient = new HttpClient();
        }

        [HttpGet]
        public async Task<IActionResult> Get()
        {
            try
            {
                var apiKey = _config["Gemini:ApiKey"];

                if (string.IsNullOrEmpty(apiKey))
                {
                    return BadRequest("Clé API Gemini manquante.");
                }

                var prompt = new
                {
                    contents = new[]
                    {
                        new
                        {
                            parts = new[]
                            {
                                new
                                {
                                    text = "Donne une analyse courte sur les tendances des ventes dans une entreprise tech."
                                }
                            }
                        }
                    }
                };

                var json = JsonSerializer.Serialize(prompt);

                var request = new HttpRequestMessage(
                    HttpMethod.Post,
                    $"https://generativelanguage.googleapis.com/v1/models/gemini-2.5-flash:generateContent?key={apiKey}"
                );

                request.Content = new StringContent(json, Encoding.UTF8, "application/json");

                var response = await _httpClient.SendAsync(request);

                if (!response.IsSuccessStatusCode)
                {
                    var error = await response.Content.ReadAsStringAsync();

                    return StatusCode((int)response.StatusCode, new
                    {
                        error = "Erreur Gemini API",
                        details = error
                    });
                }

                var result = await response.Content.ReadAsStringAsync();

                using var doc = JsonDocument.Parse(result);

                var text = doc.RootElement
                    .GetProperty("candidates")[0]
                    .GetProperty("content")
                    .GetProperty("parts")[0]
                    .GetProperty("text")
                    .GetString();

                return Ok(new
                {
                    success = true,
                    model = "gemini-2.5-flash",
                    insight = text
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new
                {
                    error = "Erreur serveur",
                    message = ex.Message
                });
            }
        }
    }
}