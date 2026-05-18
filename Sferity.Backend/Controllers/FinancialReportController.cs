using Microsoft.AspNetCore.Mvc;
using System.Text.Json;
using System.IO;
using System.Threading.Tasks;
using Sferity.Backend.Models;
using Sferity.Backend.PdfDocuments;
using RaportAI = Sferity.Backend.Servises.RaportAI;

namespace Sferity.Backend.Controllers;

[ApiController]
[Route("[controller]")]

public class FinancialReportController : ControllerBase
{
    
    [HttpPost("reportAnalyze")]
    public async Task<IActionResult> Analyze(IFormFile file)
    {
        if (file.Length == 0)
        {
            return BadRequest("Plik pusty");
        }

        try
        {
            using (var reader = new StreamReader(file.OpenReadStream()))
            {
                var jsonContent = await reader.ReadToEndAsync();
                
                var krsReport = JsonSerializer.Deserialize<KRSReport>(jsonContent,  new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

                if (krsReport == null)
                {
                    return BadRequest(krsReport.ToString());
                }
                return Ok(krsReport);
            }
        }
        catch (JsonException ex)
        {
            return BadRequest(ex.Message);
        }
        catch( Exception ex)
        {
            return BadRequest(ex.Message);
        }
    }
  [HttpGet("analyze-nip/{nip}")]
public async Task<IActionResult> AnalyzeByNip(string nip)
{
    if (string.IsNullOrWhiteSpace(nip)) return BadRequest("NIP nie może być pusty.");
    nip = nip.Replace("-", "").Replace(" ", "");

    try
    {
        using var httpClient = new HttpClient();
        
        // --- ETAP 1: BIAŁA LISTA (Pobieramy KRS na podstawie NIP) ---
        string today = DateTime.Now.ToString("yyyy-MM-dd");
        string wlApiUrl = $"https://wl-api.mf.gov.pl/api/search/nip/{nip}?date={today}";
        
        var wlResponse = await httpClient.GetAsync(wlApiUrl);
        if (!wlResponse.IsSuccessStatusCode) return NotFound("Nie znaleziono NIP w Białej Liście MF.");

        var wlJson = await wlResponse.Content.ReadAsStringAsync();
        using var wlDoc = JsonDocument.Parse(wlJson);
        var subject = wlDoc.RootElement.GetProperty("result").GetProperty("subject");

        string nazwaFirmy = subject.GetProperty("name").GetString() ?? "Brak nazwy";
        string krs = "Brak wpisu";
        
        if (subject.TryGetProperty("krs", out var krsProp) && krsProp.ValueKind != JsonValueKind.Null)
        {
            krs = krsProp.GetString() ?? "Brak wpisu";
        }

        // --- ETAP 2: API KRS (Pobieramy pełne dane o spółce, jeśli ma KRS) ---
        string krsOdpisJson = "{}"; // Domyślnie puste
        
        if (krs != "Brak wpisu" && krs.Length == 10)
        {
            // Oficjalne API Ministerstwa Sprawiedliwości
            string krsApiUrl = $"https://api-krs.ms.gov.pl/api/krs/OdpisAktualny/{krs}?rejestr=P&format=json";
            var krsResponse = await httpClient.GetAsync(krsApiUrl);
            
            if (krsResponse.IsSuccessStatusCode)
            {
                // Pobieramy surowy JSON prosto z rządu!
                krsOdpisJson = await krsResponse.Content.ReadAsStringAsync();
            }
        }

        // --- ETAP 3: AI Wkracza do akcji ---
        // Skoro mamy już pełne dane z KRS, możemy wysłać je do naszej klasy AI!
        using var krsJsonDoc = JsonDocument.Parse(krsOdpisJson);
        string aiOcenaMarkdown = await _finance.GenerateRaport(krsJsonDoc.RootElement);

        // --- ETAP 4: Pakujemy wszystko i wysyłamy na Front do Vue ---
        var reportData = new
        {
            companyName = nazwaFirmy,
            nip = nip,
            krs = krs,
            // Przekazujemy ocenę wygenerowaną przez klasę RaportAI
            aiRaport = new 
            {
                markdownContent = aiOcenaMarkdown
            },
            // Możemy też wysłać surowe dane z KRS, żeby je pokazać w tabelach!
            rawKrsData = krsOdpisJson 
        };

        return Ok(reportData);
    }
    catch (Exception ex)
    {
        return BadRequest($"Wystąpił błąd podczas analizy: {ex.Message}");
    }
}

    private readonly RaportAI _finance;
    public FinancialReportController(RaportAI finance)
    {
        _finance = finance;
    }
    [HttpPost("generate-report")]
    public async Task<IActionResult> GenerateReport([FromBody] JsonElement jsonData)
    {
        try
        {
            // 1. Wyciągamy dane z Vue
            string nip = jsonData.TryGetProperty("nip", out var n) ? n.GetString() ?? "Brak" : "Brak";
            string companyName = jsonData.TryGetProperty("companyName", out var c) ? c.GetString() ?? "Nieznana firma" : "Nieznana firma";

            // 2. Generujemy AI
            string aiReportText = await _finance.GenerateRaport(jsonData);
        
            // 3. Usuwamy znaczki Markdown, żeby PDF się nie "krztusił" surowym kodem
            string cleanAiText = aiReportText.Replace("### ", "").Replace("**", "").Replace("* ", "- ");

            // 4. Przekazujemy CZYSTE stringi do generatora (omijając w ogóle zepsuty model KRSReport!)
            var document = new Sferity.Backend.PdfDocuments.KRSReportDocument(companyName, nip, cleanAiText);
            byte[] pdfBytes = document.GeneratePdf();

            return File(pdfBytes, "application/pdf", $"Raport_AI_{nip}.pdf");
        }
        catch (Exception ex)
        {
            return BadRequest($"Błąd podczas generowania pliku PDF: {ex.Message}");
        }
    }
    
    
}