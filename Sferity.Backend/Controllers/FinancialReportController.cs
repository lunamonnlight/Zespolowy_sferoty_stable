using Microsoft.AspNetCore.Mvc;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc;
using System.IO;
using System.Text.Json;
using System.Threading.Tasks;
using Sferity.Backend.Models;
using Sferity.Backend.PdfDocuments;
using Sferity.Backend.Servises;



namespace Sferity.Backend.Controllers;

[ApiController]
[Route("[controller]")]

public class FinancialReportController : ControllerBase
{

    
    private readonly RaportAI _raportAI;
    private readonly string _adminDbPath = "admin_database.json";
    public FinancialReportController(RaportAI RaportAI)
    {
        _raportAI = RaportAI;
    }

   // GLOWNY KONTROLER POBIERANIA DANYCH Z FRONTENDU
    [HttpPost("reportAnalyze")]
    public async Task<IActionResult> Analyze([FromBody] ReportRequest request)
    {
        if (request == null || request.Report == null)
            return BadRequest("Brak danych");

        

        try
        {
            var report = await _raportAI.GenerateRaport(
                request.Report,
                (int)request.Report.PodstawoweInformacje.Id,
                request.Type ?? "Custom"
            );

            // LOGOWANIE
            LogSearchAction(request, true, null);

            return Ok(new {
                reportId = report.Id,
                markdown = report.MarkdownContent,
                krsDane = report.KrsReportId,
                date = report.CreatedAtUtc
            });
        }
        catch (Exception ex)
        {
            LogSearchAction(request, false, ex.Message);
            throw;
        }
    }
    
    
    // KONTROLER DLA RAPORTU AI
    [HttpGet("ai-report/{id}")]
    public IActionResult GetAIReport(int id)
    {
        var report = _raportAI.GetReport(id);

        if (report == null)
            return NotFound();

        return Ok(new
        {
            report.Id,
            report.MarkdownContent,
            report.CreatedAtUtc
        });
    }
    // KONTROLER DLA POBIERANIA RAPORTU
    [HttpPost("exportPdf")]
    public async Task<IActionResult> ExportPdf([FromBody] ReportRequest request)
    {
        if (request == null || request.Report == null)
            return BadRequest();

        var aiReport = await _raportAI.GenerateRaport(
            request.Report,
            request.Report.PodstawoweInformacje.Id,
            request.Type ?? "Custom"
        );

        var pdfService = new KRSReportDocument();
        var pdfBytes = await pdfService.GenerateReport(
            request.Report,
            aiReport.MarkdownContent,
            request.Type ?? "Custom" 
        );

        return File(pdfBytes, "application/pdf", "raport.pdf");
    }
    
    private void LogSearchAction(ReportRequest request, bool success, string? error)
    {
        string _adminDbPath = "admin_database.json";
        if (!System.IO.File.Exists(_adminDbPath)) return;

        var json = System.IO.File.ReadAllText(_adminDbPath);
        var db = JsonSerializer.Deserialize<AdminStore>(json);
        if (db == null) return;

        // Tworzenie wpisu do logów
        var newLog = new SearchLog
        {
            Id = db.SearchLogs.Count + 1,
            UserId = request.UserId,
            Username = request.Username,
            SearchedNip = request.Report.PodstawoweInformacje.Numery?.Nip.ToString(), // Sprawdź czy Numery czy numery
            SearchedKrs = request.Report.PodstawoweInformacje.Numery?.Krs,
            SearchTimestamp = DateTime.UtcNow,
            IsSuccess = success,
            ErrorMessage = error,
            Cost = 10.00m, // KOSZT RAPORTU
            ReportAI = true,
            Financial = request.SelectedItems.Contains("raporty")
        };

        db.SearchLogs.Insert(0, newLog); // Dodaj log na górę listy

        // ODEJMOWANIE Z KONTA:
        var userFund = db.Funds.FirstOrDefault(f => f.UserId == request.UserId);
        if (userFund != null) {
            userFund.Amount -= newLog.Cost;
        }

        // Zapisz zmiany do pliku
        var updatedJson = JsonSerializer.Serialize(db, new JsonSerializerOptions { WriteIndented = true });
        System.IO.File.WriteAllText(_adminDbPath, updatedJson);
    }
  
}