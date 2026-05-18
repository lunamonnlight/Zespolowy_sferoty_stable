using System.IO;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
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

   
    [HttpPost("reportAnalyze")]
    public async Task<IActionResult> Analyze([FromBody] KRSReport krsReport)
    {
        if (krsReport == null)
            return BadRequest("Brak danych");

        var report = await _raportAI.GenerateRaport(krsReport, krsReport.PodstawoweInformacje.Id);
        
        
        return Ok(new
        {
            reportId = report.Id,
            markdown = report.MarkdownContent,
            krsDane = report.KrsReportId,
            date = report.CreatedAtUtc
        });
    }

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
    
    [HttpPost("exportPdf")]
    public IActionResult ExportPdf([FromBody] KRSReport krsReport)
    {
        if (krsReport == null)
            return BadRequest();

        var pdfService = new KRSReportDocument();
        var pdfBytes = pdfService.GenerateReport(krsReport);

        return File(pdfBytes, "application/pdf", "raport.pdf");
    }
    
    
    
}