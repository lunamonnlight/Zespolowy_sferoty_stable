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

        

        var report = await _raportAI.GenerateRaport(
            request.Report,
            (int)request.Report.PodstawoweInformacje.Id,
            request.Type ?? "Custom"
        );

        return Ok(new
        {
            reportId = report.Id,
            markdown = report.MarkdownContent,
            krsDane = report.KrsReportId,
            date = report.CreatedAtUtc
        });
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
    
    
    
}