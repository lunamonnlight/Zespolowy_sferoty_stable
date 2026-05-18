using System.Net.Http.Headers;
using System.Text.Json;
using System.Text.Json.Serialization;
using Sferity.Backend.Models;

namespace Sferity.Backend.Servises;

public class RaportAI
{
    private readonly HttpClient _httpClient;
    private readonly IConfiguration _config;
    

    // W pamięci przechowywane raporty
    private readonly Dictionary<int, AIRaport> _reports = new();
    private int _nextId = 1; // automatyczne ID

    public RaportAI(HttpClient httpClient, IConfiguration config)
    {
        _httpClient = httpClient;
        _config = config;
    }

    public async Task<AIRaport> GenerateRaport(object jsonData, int krsReportId)
    {
        var promptFile = await File.ReadAllTextAsync("Files/message.txt");
        var json = JsonSerializer.Serialize(jsonData, new JsonSerializerOptions { WriteIndented = true });
        var prompt = promptFile.Replace("{{JSON_DATA}}", json);

        var apiKey = _config["GeminiApiKey"];
        if (string.IsNullOrEmpty(apiKey))
            throw new InvalidOperationException("Gemini API Key is not configured.");

        var requestBody = new
        {
            contents = new[]
            {
                new { parts = new[] { new { text = prompt } } }
            }
        };

        using var request = new HttpRequestMessage(
            HttpMethod.Post,
            $"https://generativelanguage.googleapis.com/v1beta/models/gemini-flash-latest:generateContent?key={apiKey}"
        );

        request.Content = JsonContent.Create(requestBody);
        request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

        var response = await _httpClient.SendAsync(request);
        var result = await response.Content.ReadAsStringAsync();

        string markdown;

        if (!response.IsSuccessStatusCode)
        {
            markdown = ((int)response.StatusCode == 503)
                ? "AI chwilowo przeciążone. Spróbuj ponownie za chwilę."
                : $"Błąd API ({(int)response.StatusCode}): {response.ReasonPhrase}";
        }
        else
        {
            try
            {
                using var doc = JsonDocument.Parse(result);
                markdown = doc.RootElement
                    .GetProperty("candidates")[0]
                    .GetProperty("content")
                    .GetProperty("parts")[0]
                    .GetProperty("text")
                    .GetString() ?? string.Empty;
            }
            catch
            {
                markdown = result;
            }
        }

        var report = new AIRaport
        {
            Id = _nextId++,
            KrsReportId = krsReportId,
            MarkdownContent = markdown,
            CreatedAtUtc = DateTime.UtcNow
        };

        _reports[report.Id] = report; // zapis do pamięci
        return report;
    }

    public AIRaport? GetReport(int id)
    {
        _reports.TryGetValue(id, out var report);
        return report;
    }
}